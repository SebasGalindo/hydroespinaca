using ChatbotService.Application.Features.Chat.Commands.DeleteSession;
using ChatbotService.Domain.Entities;
using ChatbotService.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace ChatbotService.Application.Tests.Features.Chat;

public class DeleteSessionCommandHandlerTests
{
    private readonly Mock<IChatSessionRepository> _sessionRepoMock;
    private readonly Mock<ILogger<DeleteSessionCommandHandler>> _loggerMock;
    private readonly DeleteSessionCommandHandler _handler;

    public DeleteSessionCommandHandlerTests()
    {
        _sessionRepoMock = new Mock<IChatSessionRepository>();
        _loggerMock = new Mock<ILogger<DeleteSessionCommandHandler>>();
        _handler = new DeleteSessionCommandHandler(
            _sessionRepoMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_ValidOwnership_ShouldDeleteAndReturnTrue()
    {
        // Arrange
        var session = new ChatSession { UserId = "user-1" };
        session.SetId("session-1");

        _sessionRepoMock.Setup(x => x.GetByIdAsync("session-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);
        _sessionRepoMock.Setup(x => x.DeleteAsync("session-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var command = new DeleteSessionCommand("session-1", "user-1");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        _sessionRepoMock.Verify(x => x.DeleteAsync("session-1", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_SessionNotFound_ShouldReturnFalse()
    {
        // Arrange
        _sessionRepoMock.Setup(x => x.GetByIdAsync("nonexistent", It.IsAny<CancellationToken>()))
            .ReturnsAsync((ChatSession?)null);

        var command = new DeleteSessionCommand("nonexistent", "user-1");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeFalse();
        _sessionRepoMock.Verify(x => x.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WrongOwner_ShouldReturnFalse()
    {
        // Arrange
        var session = new ChatSession { UserId = "user-1" };
        session.SetId("session-1");

        _sessionRepoMock.Setup(x => x.GetByIdAsync("session-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        var command = new DeleteSessionCommand("session-1", "user-DIFFERENT");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeFalse();
        _sessionRepoMock.Verify(x => x.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_DeleteFails_ShouldReturnFalse()
    {
        // Arrange
        var session = new ChatSession { UserId = "user-1" };
        session.SetId("session-1");

        _sessionRepoMock.Setup(x => x.GetByIdAsync("session-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);
        _sessionRepoMock.Setup(x => x.DeleteAsync("session-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var command = new DeleteSessionCommand("session-1", "user-1");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeFalse();
    }
}
