using ChatbotService.Application.Features.Chat.Queries.GetSessionMessages;
using ChatbotService.Domain.Entities;
using ChatbotService.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace ChatbotService.Application.Tests.Features.Chat;

public class GetSessionMessagesQueryHandlerTests
{
    private readonly Mock<IChatSessionRepository> _sessionRepoMock;
    private readonly Mock<ILogger<GetSessionMessagesQueryHandler>> _loggerMock;
    private readonly GetSessionMessagesQueryHandler _handler;

    public GetSessionMessagesQueryHandlerTests()
    {
        _sessionRepoMock = new Mock<IChatSessionRepository>();
        _loggerMock = new Mock<ILogger<GetSessionMessagesQueryHandler>>();
        _handler = new GetSessionMessagesQueryHandler(
            _sessionRepoMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_ValidSession_ShouldReturnMessageDtos()
    {
        // Arrange
        var session = new ChatSession { UserId = "user-1" };
        session.SetId("session-1");
        session.AddMessage(new ChatMessage { Role = "user", Content = "¿Qué temperatura hay?" });
        session.AddMessage(new ChatMessage { Role = "model", Content = "La temperatura promedio es 28°C", TokensUsed = 15 });

        _sessionRepoMock
            .Setup(x => x.GetByIdAsync("session-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        var query = new GetSessionMessagesQuery("session-1", "user-1");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        result[0].Role.Should().Be("user");
        result[0].Content.Should().Contain("temperatura");
        result[0].TokensUsed.Should().BeNull();
        result[1].Role.Should().Be("model");
        result[1].TokensUsed.Should().Be(15);
    }

    [Fact]
    public async Task Handle_SessionNotFound_ShouldReturnEmptyList()
    {
        // Arrange
        _sessionRepoMock
            .Setup(x => x.GetByIdAsync("nonexistent", It.IsAny<CancellationToken>()))
            .ReturnsAsync((ChatSession?)null);

        var query = new GetSessionMessagesQuery("nonexistent", "user-1");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WrongOwner_ShouldReturnEmptyList()
    {
        // Arrange
        var session = new ChatSession { UserId = "owner-1" };
        session.SetId("session-1");
        session.AddMessage(new ChatMessage { Role = "user", Content = "Datos secretos" });

        _sessionRepoMock
            .Setup(x => x.GetByIdAsync("session-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        var query = new GetSessionMessagesQuery("session-1", "intruder-999");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeEmpty("the user does not own this session");
    }

    [Fact]
    public async Task Handle_EmptySession_ShouldReturnEmptyList()
    {
        // Arrange
        var session = new ChatSession { UserId = "user-1" };
        session.SetId("session-1");

        _sessionRepoMock
            .Setup(x => x.GetByIdAsync("session-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        var query = new GetSessionMessagesQuery("session-1", "user-1");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }
}
