using ChatbotService.Application.Features.Chat.Commands.SendMessage;
using ChatbotService.Domain.Entities;
using ChatbotService.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace ChatbotService.Application.Tests.Features.Chat;

public class SendMessageCommandHandlerTests
{
    private readonly Mock<IChatSessionRepository> _sessionRepoMock;
    private readonly Mock<IEmbeddingProvider> _embeddingMock;
    private readonly Mock<IVectorStore> _vectorStoreMock;
    private readonly Mock<ILiveContextProvider> _liveContextMock;
    private readonly Mock<ILlmProvider> _llmMock;
    private readonly Mock<ILogger<SendMessageCommandHandler>> _loggerMock;
    private readonly SendMessageCommandHandler _handler;

    public SendMessageCommandHandlerTests()
    {
        _sessionRepoMock = new Mock<IChatSessionRepository>();
        _embeddingMock = new Mock<IEmbeddingProvider>();
        _vectorStoreMock = new Mock<IVectorStore>();
        _liveContextMock = new Mock<ILiveContextProvider>();
        _llmMock = new Mock<ILlmProvider>();
        _loggerMock = new Mock<ILogger<SendMessageCommandHandler>>();
        _handler = new SendMessageCommandHandler(
            _sessionRepoMock.Object,
            _embeddingMock.Object,
            _vectorStoreMock.Object,
            _liveContextMock.Object,
            _llmMock.Object,
            _loggerMock.Object);
    }

    private ChatSession CreateValidSession(string userId = "user-1", string sessionId = "session-1")
    {
        var session = new ChatSession { UserId = userId };
        session.SetId(sessionId);
        return session;
    }

    private void SetupDefaultMocks(ChatSession session, string userMessage = "¿Qué temperatura hay?")
    {
        _sessionRepoMock
            .Setup(x => x.GetByIdAsync(session.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);
        _sessionRepoMock
            .Setup(x => x.AddMessageToSessionAsync(It.IsAny<string>(), It.IsAny<ChatMessage>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _sessionRepoMock
            .Setup(x => x.UpdateTitleAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _embeddingMock
            .Setup(x => x.GenerateEmbeddingAsync(userMessage, EmbeddingTaskType.RetrievalQuery, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new float[] { 0.1f, 0.2f });
        _vectorStoreMock
            .Setup(x => x.SearchSimilarAsync(It.IsAny<float[]>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<KnowledgeChunk>());
        _liveContextMock
            .Setup(x => x.GetSensorReadingsSummaryAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("[Sin datos disponibles]");
        _liveContextMock
            .Setup(x => x.GetRecentEvaluationsSummaryAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("[Sin evaluaciones recientes]");
        _liveContextMock
            .Setup(x => x.GetActuatorStatesSummaryAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync("[Sin actuadores]");
    }

    private static async IAsyncEnumerable<string> MockTokenStream(params string[] tokens)
    {
        foreach (var token in tokens)
        {
            yield return token;
            await Task.Yield();
        }
    }

    [Fact]
    public async Task Handle_SessionNotFound_ShouldThrowInvalidOperationException()
    {
        // Arrange
        _sessionRepoMock
            .Setup(x => x.GetByIdAsync("nonexistent", It.IsAny<CancellationToken>()))
            .ReturnsAsync((ChatSession?)null);

        var command = new SendMessageCommand("nonexistent", "user-1", "Hola", null);

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*no encontrada*");
    }

    [Fact]
    public async Task Handle_WrongOwner_ShouldThrowUnauthorizedAccessException()
    {
        // Arrange
        var session = CreateValidSession("owner-1", "session-1");
        _sessionRepoMock
            .Setup(x => x.GetByIdAsync("session-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        var command = new SendMessageCommand("session-1", "intruder-999", "Hola", null);

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*no pertenece*");
    }

    [Fact]
    public async Task Handle_ValidRequest_ShouldPersistUserMessage()
    {
        // Arrange
        var session = CreateValidSession();
        SetupDefaultMocks(session);

        _llmMock
            .Setup(x => x.GenerateStreamAsync(It.IsAny<string>(), It.IsAny<List<ChatMessage>>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(MockTokenStream("Respuesta"));

        var command = new SendMessageCommand("session-1", "user-1", "¿Qué temperatura hay?", null);

        // Act
        var stream = await _handler.Handle(command, CancellationToken.None);
        // Consume the stream to trigger side-effects
        await foreach (var _ in stream) { }

        // Assert — user message should be persisted first
        _sessionRepoMock.Verify(
            x => x.AddMessageToSessionAsync("session-1",
                It.Is<ChatMessage>(m => m.Role == "user" && m.Content == "¿Qué temperatura hay?"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ValidRequest_ShouldReturnStreamTokens()
    {
        // Arrange
        var session = CreateValidSession();
        SetupDefaultMocks(session);

        _llmMock
            .Setup(x => x.GenerateStreamAsync(It.IsAny<string>(), It.IsAny<List<ChatMessage>>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(MockTokenStream("La ", "temperatura ", "es ", "28°C."));

        var command = new SendMessageCommand("session-1", "user-1", "¿Qué temperatura hay?", null);

        // Act
        var stream = await _handler.Handle(command, CancellationToken.None);
        var tokens = new List<string>();
        await foreach (var token in stream)
        {
            tokens.Add(token);
        }

        // Assert
        tokens.Should().HaveCount(4);
        string.Concat(tokens).Should().Be("La temperatura es 28°C.");
    }

    [Fact]
    public async Task Handle_ValidRequest_ShouldPersistModelResponseAfterStream()
    {
        // Arrange
        var session = CreateValidSession();
        SetupDefaultMocks(session);

        _llmMock
            .Setup(x => x.GenerateStreamAsync(It.IsAny<string>(), It.IsAny<List<ChatMessage>>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(MockTokenStream("Token1", "Token2", "Token3"));

        var command = new SendMessageCommand("session-1", "user-1", "¿Qué temperatura hay?", null);

        // Act
        var stream = await _handler.Handle(command, CancellationToken.None);
        await foreach (var _ in stream) { }

        // Assert — model message should be persisted after stream completes
        _sessionRepoMock.Verify(
            x => x.AddMessageToSessionAsync("session-1",
                It.Is<ChatMessage>(m =>
                    m.Role == "model" &&
                    m.Content == "Token1Token2Token3" &&
                    m.TokensUsed == 3),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_FirstMessage_ShouldAutoGenerateTitle()
    {
        // Arrange
        var session = CreateValidSession();
        // Session has no messages (first interaction) — Messages.Count will be 0
        SetupDefaultMocks(session);

        _llmMock
            .Setup(x => x.GenerateStreamAsync(It.IsAny<string>(), It.IsAny<List<ChatMessage>>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(MockTokenStream("Ok"));

        var command = new SendMessageCommand("session-1", "user-1", "¿Qué temperatura hay?", null);

        // Act
        var stream = await _handler.Handle(command, CancellationToken.None);
        await foreach (var _ in stream) { }

        // Assert
        _sessionRepoMock.Verify(
            x => x.UpdateTitleAsync("session-1", "¿Qué temperatura hay?", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_FirstMessage_LongText_ShouldTruncateTitleTo50Chars()
    {
        // Arrange
        var session = CreateValidSession();
        var longMessage = new string('X', 100);
        SetupDefaultMocks(session, longMessage);

        _llmMock
            .Setup(x => x.GenerateStreamAsync(It.IsAny<string>(), It.IsAny<List<ChatMessage>>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(MockTokenStream("Ok"));

        var command = new SendMessageCommand("session-1", "user-1", longMessage, null);

        // Act
        var stream = await _handler.Handle(command, CancellationToken.None);
        await foreach (var _ in stream) { }

        // Assert — title should be truncated to 50 chars + "..."
        _sessionRepoMock.Verify(
            x => x.UpdateTitleAsync("session-1",
                It.Is<string>(t => t.Length == 53 && t.EndsWith("...")),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_SecondMessage_ShouldNotUpdateTitle()
    {
        // Arrange
        var session = CreateValidSession();
        // Add a previous message to simulate this is NOT the first interaction
        session.AddMessage(new ChatMessage { Role = "user", Content = "Pregunta anterior" });
        session.AddMessage(new ChatMessage { Role = "model", Content = "Respuesta anterior" });
        SetupDefaultMocks(session);

        _llmMock
            .Setup(x => x.GenerateStreamAsync(It.IsAny<string>(), It.IsAny<List<ChatMessage>>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(MockTokenStream("Ok"));

        var command = new SendMessageCommand("session-1", "user-1", "¿Qué temperatura hay?", null);

        // Act
        var stream = await _handler.Handle(command, CancellationToken.None);
        await foreach (var _ in stream) { }

        // Assert
        _sessionRepoMock.Verify(
            x => x.UpdateTitleAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldCallEmbeddingWithRetrievalQuery()
    {
        // Arrange
        var session = CreateValidSession();
        SetupDefaultMocks(session);

        _llmMock
            .Setup(x => x.GenerateStreamAsync(It.IsAny<string>(), It.IsAny<List<ChatMessage>>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(MockTokenStream("Ok"));

        var command = new SendMessageCommand("session-1", "user-1", "¿Qué temperatura hay?", null);

        // Act
        var stream = await _handler.Handle(command, CancellationToken.None);
        await foreach (var _ in stream) { }

        // Assert
        _embeddingMock.Verify(
            x => x.GenerateEmbeddingAsync("¿Qué temperatura hay?", EmbeddingTaskType.RetrievalQuery, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldCallVectorSearchWithTop5()
    {
        // Arrange
        var session = CreateValidSession();
        SetupDefaultMocks(session);

        _llmMock
            .Setup(x => x.GenerateStreamAsync(It.IsAny<string>(), It.IsAny<List<ChatMessage>>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(MockTokenStream("Ok"));

        var command = new SendMessageCommand("session-1", "user-1", "¿Qué temperatura hay?", null);

        // Act
        var stream = await _handler.Handle(command, CancellationToken.None);
        await foreach (var _ in stream) { }

        // Assert
        _vectorStoreMock.Verify(
            x => x.SearchSimilarAsync(It.IsAny<float[]>(), 5, null, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldCallAllLiveContextProviderMethods()
    {
        // Arrange
        var session = CreateValidSession();
        SetupDefaultMocks(session);

        _llmMock
            .Setup(x => x.GenerateStreamAsync(It.IsAny<string>(), It.IsAny<List<ChatMessage>>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(MockTokenStream("Ok"));

        var command = new SendMessageCommand("session-1", "user-1", "¿Qué temperatura hay?", null);

        // Act
        var stream = await _handler.Handle(command, CancellationToken.None);
        await foreach (var _ in stream) { }

        // Assert
        _liveContextMock.Verify(x => x.GetSensorReadingsSummaryAsync(24, It.IsAny<CancellationToken>()), Times.Once);
        _liveContextMock.Verify(x => x.GetRecentEvaluationsSummaryAsync(24, It.IsAny<CancellationToken>()), Times.Once);
        _liveContextMock.Verify(x => x.GetActuatorStatesSummaryAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
