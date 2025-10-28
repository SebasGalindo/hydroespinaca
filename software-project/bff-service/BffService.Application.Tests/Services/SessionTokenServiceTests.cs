using BffService.Application.Interfaces;
using BffService.Application.Services;
using BffService.Domain.Entities;
using BffService.Domain.Exceptions;
using BffService.Domain.Interfaces;
using BffService.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace BffService.Application.Tests.Services;

public class SessionTokenServiceTests
{
    private readonly Mock<ISessionService> _mockSessionService;
    private readonly Mock<IAuthService> _mockAuthService;
    private readonly Mock<ILogger<SessionTokenService>> _mockLogger;
    private readonly SessionTokenService _service;

    public SessionTokenServiceTests()
    {
        _mockSessionService = new Mock<ISessionService>();
        _mockAuthService = new Mock<IAuthService>();
        _mockLogger = new Mock<ILogger<SessionTokenService>>();
        _service = new SessionTokenService(
            _mockSessionService.Object,
            _mockAuthService.Object,
            _mockLogger.Object
        );
    }

    [Fact]
    public async Task GetSessionWithValidTokensAsync_WithNonExistentSession_ThrowsSessionNotFoundException()
    {
        // Arrange
        var sessionId = "non-existent-session";
        _mockSessionService.Setup(x => x.GetFullSessionAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Session?)null);

        // Act & Assert
        await Assert.ThrowsAsync<SessionNotFoundException>(
            () => _service.GetSessionWithValidTokensAsync(sessionId, CancellationToken.None)
        );
    }

    [Fact]
    public async Task GetSessionWithValidTokensAsync_WithValidSession_ReturnsSession()
    {
        // Arrange
        var sessionId = "valid-session-id";
        var session = CreateValidSession(sessionId);

        _mockSessionService.Setup(x => x.GetFullSessionAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        // Act
        var result = await _service.GetSessionWithValidTokensAsync(sessionId, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.SessionId.Should().Be(sessionId);
        result.AccessToken.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetSessionWithValidTokensAsync_WithExpiredSessionAndCannotRefresh_ThrowsSessionExpiredException()
    {
        // Arrange
        var sessionId = "expired-session";
        var session = CreateExpiredSessionWithoutRefreshToken(sessionId);

        _mockSessionService.Setup(x => x.GetFullSessionAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        // Act & Assert
        await Assert.ThrowsAsync<SessionExpiredException>(
            () => _service.GetSessionWithValidTokensAsync(sessionId, CancellationToken.None)
        );
    }

    [Fact]
    public async Task GetSessionWithValidTokensAsync_WithExpiredSessionAndValidRefreshToken_RefreshesToken()
    {
        // Arrange
        var sessionId = "expired-refreshable-session";
        var expiredSession = CreateExpiredSessionWithValidRefreshToken(sessionId);
        var newTokenInfo = new TokenInfo(
            "new-access-token",
            "new-refresh-token",
            DateTime.UtcNow.AddHours(1),
            DateTime.UtcNow.AddDays(7)
        );

        _mockSessionService.SetupSequence(x => x.GetFullSessionAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expiredSession)
            .ReturnsAsync(expiredSession);

        _mockAuthService.Setup(x => x.RefreshTokenAsync(
            It.IsAny<string>(),
            sessionId,
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(newTokenInfo);

        _mockSessionService.Setup(x => x.UpdateSessionAsync(
            It.IsAny<Session>(),
            It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.GetSessionWithValidTokensAsync(sessionId, CancellationToken.None);

        // Assert
        _mockAuthService.Verify(x => x.RefreshTokenAsync(
            It.IsAny<string>(),
            sessionId,
            It.IsAny<CancellationToken>()),
            Times.Once);

        _mockSessionService.Verify(x => x.UpdateSessionAsync(
            It.IsAny<Session>(),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetSessionWithValidTokensAsync_WithRefreshTokenFailure_ThrowsException()
    {
        // Arrange
        var sessionId = "refresh-fail-session";
        var expiredSession = CreateExpiredSessionWithValidRefreshToken(sessionId);

        _mockSessionService.SetupSequence(x => x.GetFullSessionAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expiredSession)
            .ReturnsAsync(expiredSession);

        _mockAuthService.Setup(x => x.RefreshTokenAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Refresh failed"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(
            () => _service.GetSessionWithValidTokensAsync(sessionId, CancellationToken.None)
        );
    }

    [Fact]
    public async Task GetSessionWithValidTokensAsync_WithEmptyAccessToken_ThrowsInvalidTokenException()
    {
        // Arrange
        var sessionId = "invalid-token-session";
        var session = Session.Create(sessionId, "csrf-token");
        session.SetTokens("", "refresh-token", DateTime.UtcNow.AddHours(1), DateTime.UtcNow.AddDays(7));

        _mockSessionService.Setup(x => x.GetFullSessionAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidTokenException>(
            () => _service.GetSessionWithValidTokensAsync(sessionId, CancellationToken.None)
        );
    }

    [Fact]
    public async Task GetSessionWithValidTokensAsync_WithNewRefreshToken_UpdatesBothTokens()
    {
        // Arrange
        var sessionId = "update-both-tokens-session";
        var expiredSession = CreateExpiredSessionWithValidRefreshToken(sessionId);
        var newTokenInfo = new TokenInfo(
            "new-access-token",
            "new-refresh-token",
            DateTime.UtcNow.AddHours(1),
            DateTime.UtcNow.AddDays(7)
        );

        _mockSessionService.SetupSequence(x => x.GetFullSessionAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expiredSession)
            .ReturnsAsync(expiredSession);

        _mockAuthService.Setup(x => x.RefreshTokenAsync(
            It.IsAny<string>(),
            sessionId,
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(newTokenInfo);

        Session? updatedSession = null;
        _mockSessionService.Setup(x => x.UpdateSessionAsync(
            It.IsAny<Session>(),
            It.IsAny<CancellationToken>()))
            .Callback<Session, CancellationToken>((s, _) => updatedSession = s)
            .Returns(Task.CompletedTask);

        // Act
        await _service.GetSessionWithValidTokensAsync(sessionId, CancellationToken.None);

        // Assert
        updatedSession.Should().NotBeNull();
        updatedSession!.AccessToken.Should().Be("new-access-token");
        updatedSession.RefreshToken.Should().Be("new-refresh-token");
    }

    [Fact]
    public async Task GetSessionWithValidTokensAsync_WhenSessionDisappearsAfterLockAcquired_ThrowsSessionNotFoundException()
    {
        // Arrange
        var sessionId = "disappearing-session";
        var expiredSession = CreateExpiredSessionWithValidRefreshToken(sessionId);

        _mockSessionService.SetupSequence(x => x.GetFullSessionAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expiredSession)
            .ReturnsAsync((Session?)null);

        // Act & Assert
        await Assert.ThrowsAsync<SessionNotFoundException>(
            () => _service.GetSessionWithValidTokensAsync(sessionId, CancellationToken.None)
        );
    }

    [Fact]
    public async Task GetSessionWithValidTokensAsync_WhenSessionExpiresWhileWaitingForLock_ThrowsSessionExpiredException()
    {
        // Arrange
        var sessionId = "expires-during-lock-session";
        var expiredSession = CreateExpiredSessionWithValidRefreshToken(sessionId);
        var nowExpiredWithoutRefresh = CreateExpiredSessionWithoutRefreshToken(sessionId);

        _mockSessionService.SetupSequence(x => x.GetFullSessionAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expiredSession)
            .ReturnsAsync(nowExpiredWithoutRefresh);

        // Act & Assert
        await Assert.ThrowsAsync<SessionExpiredException>(
            () => _service.GetSessionWithValidTokensAsync(sessionId, CancellationToken.None)
        );
    }

    [Fact]
    public async Task GetSessionWithValidTokensAsync_WithAlreadyRefreshedSession_ReturnsWithoutRefreshing()
    {
        // Arrange
        var sessionId = "already-refreshed-session";
        var expiredSession = CreateExpiredSessionWithValidRefreshToken(sessionId);
        var refreshedSession = CreateValidSession(sessionId);

        _mockSessionService.SetupSequence(x => x.GetFullSessionAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expiredSession)
            .ReturnsAsync(refreshedSession);

        // Act
        var result = await _service.GetSessionWithValidTokensAsync(sessionId, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        _mockAuthService.Verify(x => x.RefreshTokenAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // Helper methods
    private static Session CreateValidSession(string sessionId)
    {
        var session = Session.Create(sessionId, "csrf-token");
        session.SetTokens(
            "valid-access-token",
            "valid-refresh-token",
            DateTime.UtcNow.AddHours(1),
            DateTime.UtcNow.AddDays(7)
        );
        session.SetUserInfo("user-id", "username", "user@example.com", "Admin", new List<string> { "read", "write" });
        return session;
    }

    private static Session CreateExpiredSessionWithoutRefreshToken(string sessionId)
    {
        var session = Session.Create(sessionId, "csrf-token");
        session.SetTokens(
            "expired-access-token",
            "",
            DateTime.UtcNow.AddHours(-1),
            DateTime.UtcNow.AddHours(-1)
        );
        return session;
    }

    private static Session CreateExpiredSessionWithValidRefreshToken(string sessionId)
    {
        var session = Session.Create(sessionId, "csrf-token");
        session.SetTokens(
            "expired-access-token",
            "valid-refresh-token",
            DateTime.UtcNow.AddHours(-1),
            DateTime.UtcNow.AddDays(7)
        );
        return session;
    }
}
