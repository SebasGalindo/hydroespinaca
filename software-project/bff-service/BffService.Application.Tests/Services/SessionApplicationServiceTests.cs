using BffService.Application.Services;
using BffService.Domain.Entities;
using BffService.Domain.Interfaces;
using BffService.Domain.Services;
using BffService.Domain.ValueObjects;
using HydroEspinaca.Shared.DTOs.Authentication;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BffService.Application.Tests.Services;

public class SessionApplicationServiceTests
{
    private readonly Mock<ISessionRepository> _mockSessionRepository;
    private readonly Mock<IAuthService> _mockAuthService;
    private readonly Mock<SessionValidationService> _mockValidationService;
    private readonly Mock<ILogger<SessionApplicationService>> _mockLogger;
    private readonly SessionApplicationService _service;

    public SessionApplicationServiceTests()
    {
        _mockSessionRepository = new Mock<ISessionRepository>();
        _mockAuthService = new Mock<IAuthService>();
        _mockValidationService = new Mock<SessionValidationService>();
        _mockLogger = new Mock<ILogger<SessionApplicationService>>();
        _service = new SessionApplicationService(
            _mockSessionRepository.Object,
            _mockAuthService.Object,
            _mockValidationService.Object,
            _mockLogger.Object);
    }

    [Fact]
    public void Constructor_WithValidDependencies_ShouldCreateInstance()
    {
        // Act
        var service = new SessionApplicationService(
            _mockSessionRepository.Object,
            _mockAuthService.Object,
            _mockValidationService.Object,
            _mockLogger.Object);

        // Assert
        service.Should().NotBeNull();
    }

    [Fact]
    public async Task GetSessionInfoAsync_WithNonExistentSession_ShouldReturnNull()
    {
        // Arrange
        _mockSessionRepository
            .Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Session?)null);

        // Act
        var result = await _service.GetSessionInfoAsync("non-existent", CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ShouldReturnLoginResponse()
    {
        // Arrange
        var request = new LoginRequestDto(
            "test@example.com",
            "password123",
            "client-id",
            "192.168.1.1",
            "Mozilla/5.0");

        var authResult = new AuthenticationResult(
            new TokenInfo(
                "access-token",
                "refresh-token",
                DateTime.UtcNow.AddHours(1),
                DateTime.UtcNow.AddDays(7)),
            "user-id",
            "testuser",
            "test@example.com",
            "Admin",
            new List<string> { "read", "write" });

        _mockAuthService
            .Setup(x => x.LoginAsync(
                request.Email,
                request.Password,
                request.ClientId,
                It.IsAny<string>(),
                It.IsAny<string>(),
                request.IpAddress,
                request.UserAgent,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(authResult);

        _mockSessionRepository
            .Setup(x => x.SaveAsync(It.IsAny<Session>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.LoginAsync(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.SessionId.Should().NotBeNullOrEmpty();
        result.CsrfToken.Should().NotBeNullOrEmpty();
        _mockSessionRepository.Verify(x => x.SaveAsync(It.IsAny<Session>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LogoutAsync_WithExistingSession_ShouldDeleteSession()
    {
        // Arrange
        var sessionId = "session-id";
        var session = Session.Create(sessionId, "csrf-token");
        session.SetTokens("access", "refresh", DateTime.UtcNow.AddHours(1), DateTime.UtcNow.AddDays(7));

        var request = new LogoutRequestDto(sessionId);

        _mockSessionRepository
            .Setup(x => x.GetAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        _mockAuthService
            .Setup(x => x.LogoutAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockSessionRepository
            .Setup(x => x.DeleteAsync(sessionId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _service.LogoutAsync(request, CancellationToken.None);

        // Assert
        _mockAuthService.Verify(x => x.LogoutAsync("refresh", It.IsAny<CancellationToken>()), Times.Once);
        _mockSessionRepository.Verify(x => x.DeleteAsync(sessionId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LogoutAsync_WithNonExistentSession_ShouldNotThrow()
    {
        // Arrange
        var request = new LogoutRequestDto("non-existent");

        _mockSessionRepository
            .Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Session?)null);

        // Act
        var act = async () => await _service.LogoutAsync(request, CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();
        _mockAuthService.Verify(x => x.LogoutAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockSessionRepository.Verify(x => x.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task LogoutAsync_WithSessionWithoutRefreshToken_ShouldOnlyDeleteSession()
    {
        // Arrange
        var sessionId = "session-id";
        var session = Session.Create(sessionId, "csrf-token");

        var request = new LogoutRequestDto(sessionId);

        _mockSessionRepository
            .Setup(x => x.GetAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        _mockSessionRepository
            .Setup(x => x.DeleteAsync(sessionId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _service.LogoutAsync(request, CancellationToken.None);

        // Assert
        _mockAuthService.Verify(x => x.LogoutAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockSessionRepository.Verify(x => x.DeleteAsync(sessionId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetSessionInfoAsync_WithExistingSession_ShouldReturnSessionInfo()
    {
        // Arrange
        var sessionId = "session-id";
        var session = Session.Create(sessionId, "csrf-token");
        session.SetTokens("access", "refresh", DateTime.UtcNow.AddHours(1), DateTime.UtcNow.AddDays(7));
        session.SetUserInfo("user-id", "username", "user@example.com", "Admin", new List<string> { "read" });

        _mockSessionRepository
            .Setup(x => x.GetAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        // Act
        var result = await _service.GetSessionInfoAsync(sessionId, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.SessionId.Should().Be(sessionId);
        result.UserId.Should().Be("user-id");
        result.UserRole.Should().Be("Admin");
        result.Scopes.Should().Contain("read");
    }

    [Fact]
    public async Task RefreshTokenAsync_WithValidSession_ShouldReturnTrue()
    {
        // Arrange
        var sessionId = "session-id";
        var session = Session.Create(sessionId, "csrf-token");
        session.SetTokens("old-access", "old-refresh", DateTime.UtcNow.AddMinutes(5), DateTime.UtcNow.AddDays(7));

        var request = new RefreshTokenRequestDto(sessionId);

        var newTokenInfo = new TokenInfo(
            "new-access",
            "new-refresh",
            DateTime.UtcNow.AddHours(1),
            DateTime.UtcNow.AddDays(7));

        _mockSessionRepository
            .Setup(x => x.GetAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        _mockAuthService
            .Setup(x => x.RefreshTokenAsync("old-refresh", sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(newTokenInfo);

        _mockSessionRepository
            .Setup(x => x.SaveAsync(It.IsAny<Session>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.RefreshTokenAsync(request, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        _mockSessionRepository.Verify(x => x.SaveAsync(It.IsAny<Session>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RefreshTokenAsync_WithNonExistentSession_ShouldReturnFalse()
    {
        // Arrange
        var request = new RefreshTokenRequestDto("non-existent");

        _mockSessionRepository
            .Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Session?)null);

        // Act
        var result = await _service.RefreshTokenAsync(request, CancellationToken.None);

        // Assert
        result.Should().BeFalse();
        _mockAuthService.Verify(x => x.RefreshTokenAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RefreshTokenAsync_WithExpiredRefreshToken_ShouldReturnFalse()
    {
        // Arrange
        var sessionId = "session-id";
        var session = Session.Create(sessionId, "csrf-token");
        session.SetTokens("access", "refresh", DateTime.UtcNow.AddMinutes(5), DateTime.UtcNow.AddMinutes(-1));

        var request = new RefreshTokenRequestDto(sessionId);

        _mockSessionRepository
            .Setup(x => x.GetAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        // Act
        var result = await _service.RefreshTokenAsync(request, CancellationToken.None);

        // Assert
        result.Should().BeFalse();
        _mockAuthService.Verify(x => x.RefreshTokenAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RefreshTokenAsync_WithOnlyAccessTokenUpdated_ShouldUpdateOnlyAccessToken()
    {
        // Arrange
        var sessionId = "session-id";
        var session = Session.Create(sessionId, "csrf-token");
        session.SetTokens("old-access", "old-refresh", DateTime.UtcNow.AddMinutes(5), DateTime.UtcNow.AddDays(7));

        var request = new RefreshTokenRequestDto(sessionId);

        var newTokenInfo = new TokenInfo(
            "new-access",
            string.Empty,
            DateTime.UtcNow.AddHours(1),
            DateTime.UtcNow.AddDays(7));

        _mockSessionRepository
            .Setup(x => x.GetAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        _mockAuthService
            .Setup(x => x.RefreshTokenAsync("old-refresh", sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(newTokenInfo);

        _mockSessionRepository
            .Setup(x => x.SaveAsync(It.IsAny<Session>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.RefreshTokenAsync(request, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        _mockSessionRepository.Verify(x => x.SaveAsync(It.IsAny<Session>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateSessionAsync_ShouldCreateAndReturnSessionId()
    {
        // Arrange
        _mockSessionRepository
            .Setup(x => x.SaveAsync(It.IsAny<Session>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.CreateSessionAsync(CancellationToken.None);

        // Assert
        result.Should().NotBeNullOrEmpty();
        _mockSessionRepository.Verify(x => x.SaveAsync(It.IsAny<Session>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task InvalidateSessionAsync_WithExistingSession_ShouldInvalidateAndSave()
    {
        // Arrange
        var sessionId = "session-id";
        var session = Session.Create(sessionId, "csrf-token");

        _mockSessionRepository
            .Setup(x => x.GetAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        _mockSessionRepository
            .Setup(x => x.SaveAsync(It.IsAny<Session>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _service.InvalidateSessionAsync(sessionId, CancellationToken.None);

        // Assert
        _mockSessionRepository.Verify(x => x.SaveAsync(It.IsAny<Session>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task InvalidateSessionAsync_WithNonExistentSession_ShouldNotThrow()
    {
        // Arrange
        _mockSessionRepository
            .Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Session?)null);

        // Act
        var act = async () => await _service.InvalidateSessionAsync("non-existent", CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();
        _mockSessionRepository.Verify(x => x.SaveAsync(It.IsAny<Session>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetFullSessionAsync_WithExistingSession_ShouldReturnSession()
    {
        // Arrange
        var sessionId = "session-id";
        var session = Session.Create(sessionId, "csrf-token");

        _mockSessionRepository
            .Setup(x => x.GetAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        // Act
        var result = await _service.GetFullSessionAsync(sessionId, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.SessionId.Should().Be(sessionId);
    }

    [Fact]
    public async Task GetFullSessionAsync_WithNonExistentSession_ShouldReturnNull()
    {
        // Arrange
        _mockSessionRepository
            .Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Session?)null);

        // Act
        var result = await _service.GetFullSessionAsync("non-existent", CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateSessionAsync_ShouldSaveSession()
    {
        // Arrange
        var session = Session.Create("session-id", "csrf-token");

        _mockSessionRepository
            .Setup(x => x.SaveAsync(session, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _service.UpdateSessionAsync(session, CancellationToken.None);

        // Assert
        _mockSessionRepository.Verify(x => x.SaveAsync(session, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteSessionAsync_ShouldDeleteSession()
    {
        // Arrange
        var sessionId = "session-id";

        _mockSessionRepository
            .Setup(x => x.DeleteAsync(sessionId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _service.DeleteSessionAsync(sessionId, CancellationToken.None);

        // Assert
        _mockSessionRepository.Verify(x => x.DeleteAsync(sessionId, It.IsAny<CancellationToken>()), Times.Once);
    }

}
