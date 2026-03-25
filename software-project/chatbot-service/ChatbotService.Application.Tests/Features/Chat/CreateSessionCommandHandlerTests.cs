using ChatbotService.Application.Features.Chat.Commands.CreateSession;
using ChatbotService.Domain.Entities;
using ChatbotService.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace ChatbotService.Application.Tests.Features.Chat;

public class CreateSessionCommandHandlerTests
{
    private readonly Mock<IChatSessionRepository> _sessionRepoMock;
    private readonly Mock<ILogger<CreateSessionCommandHandler>> _loggerMock;
    private readonly CreateSessionCommandHandler _handler;

    public CreateSessionCommandHandlerTests()
    {
        _sessionRepoMock = new Mock<IChatSessionRepository>();
        _loggerMock = new Mock<ILogger<CreateSessionCommandHandler>>();
        _handler = new CreateSessionCommandHandler(
            _sessionRepoMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_ValidUserId_ShouldCreateSessionAndReturnId()
    {
        // Arrange
        var command = new CreateSessionCommand("user-abc-123");

        _sessionRepoMock
            .Setup(x => x.CreateAsync(It.IsAny<ChatSession>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.SessionId.Should().NotBeNullOrWhiteSpace();
        _sessionRepoMock.Verify(
            x => x.CreateAsync(
                It.Is<ChatSession>(s =>
                    s.UserId == "user-abc-123" &&
                    s.Title == "Nueva conversación" &&
                    s.Messages.Count == 0),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldSetGuidAsSessionId()
    {
        // Arrange
        var command = new CreateSessionCommand("user-123");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        // The handler sets the Id via Guid.NewGuid().ToString()
        Guid.TryParse(result.SessionId, out _).Should().BeTrue(
            "the session ID should be a valid GUID string");
    }

    [Fact]
    public async Task Handle_ShouldCallRepositoryExactlyOnce()
    {
        // Arrange
        var command = new CreateSessionCommand("user-456");

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _sessionRepoMock.Verify(
            x => x.CreateAsync(It.IsAny<ChatSession>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
