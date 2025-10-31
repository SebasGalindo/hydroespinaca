using BffService.Domain.Entities;
using BffService.Domain.Exceptions;
using BffService.Domain.Services;

namespace BffService.Domain.Tests.Services;

public class SessionValidationServiceTests
{
    private readonly SessionValidationService _service = new();

    [Fact]
    public void IsSessionValid_WithValidSession_ShouldReturnTrue()
    {
        // Arrange
        var session = Session.Create("session-id", "csrf-token");
        session.SetTokens("valid-token", "refresh-token", DateTime.UtcNow.AddHours(1));

        // Act
        var result = _service.IsSessionValid(session);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsSessionValid_WithExpiredSession_ShouldReturnFalse()
    {
        // Arrange
        var session = Session.Create("session-id", "csrf-token");
        session.SetTokens("token", "refresh-token", DateTime.UtcNow.AddSeconds(-1));

        // Act
        var result = _service.IsSessionValid(session);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsSessionValid_WithNullSession_ShouldReturnFalse()
    {
        // Act
        var result = _service.IsSessionValid(null);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateSessionOrThrow_WithNullSession_ShouldThrowSessionNotFoundException()
    {
        // Arrange
        var sessionId = "non-existent-session";

        // Act & Assert
        var exception = Assert.Throws<SessionNotFoundException>(() =>
            _service.ValidateSessionOrThrow(null, sessionId));

        exception.Message.Should().Contain(sessionId);
    }

    [Fact]
    public void ValidateSessionOrThrow_WithExpiredSession_ShouldThrowSessionExpiredException()
    {
        // Arrange
        var session = Session.Create("session-id", "csrf-token");
        session.SetTokens("token", "refresh", DateTime.UtcNow.AddSeconds(-1));

        // Act & Assert
        var exception = Assert.Throws<SessionExpiredException>(() =>
            _service.ValidateSessionOrThrow(session, session.SessionId));

        exception.Message.Should().Contain(session.SessionId);
    }

    [Fact]
    public void ValidateSessionOrThrow_WithValidSession_ShouldNotThrow()
    {
        // Arrange
        var session = Session.Create("session-id", "csrf-token");
        session.SetTokens("token", "refresh", DateTime.UtcNow.AddHours(1));

        // Act & Assert
        var exception = Record.Exception(() =>
            _service.ValidateSessionOrThrow(session, session.SessionId));

        exception.Should().BeNull();
    }

    [Fact]
    public void RequiresRefresh_WithExpiredSessionAndValidRefreshToken_ShouldReturnTrue()
    {
        // Arrange
        var session = Session.Create("session-id", "csrf-token");
        session.SetTokens("token", "refresh-token", DateTime.UtcNow.AddSeconds(-1), DateTime.UtcNow.AddDays(7));

        // Act
        var result = _service.RequiresRefresh(session);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void RequiresRefresh_WithExpiredSessionAndExpiredRefreshToken_ShouldReturnFalse()
    {
        // Arrange
        var session = Session.Create("session-id", "csrf-token");
        session.SetTokens("token", "refresh-token", DateTime.UtcNow.AddSeconds(-1), DateTime.UtcNow.AddSeconds(-1));

        // Act
        var result = _service.RequiresRefresh(session);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void RequiresRefresh_WithSessionExpiringIn10Minutes_ShouldReturnFalse()
    {
        // Arrange
        var session = Session.Create("session-id", "csrf-token");
        session.SetTokens("token", "refresh-token", DateTime.UtcNow.AddMinutes(10), DateTime.UtcNow.AddDays(7));

        // Act
        var result = _service.RequiresRefresh(session);

        // Assert - Should be false because session doesn't expire soon (needs <30 seconds)
        result.Should().BeFalse();
    }

    [Fact]
    public void RequiresRefresh_WithSessionExpiringIn20Seconds_ShouldReturnTrue()
    {
        // Arrange
        var session = Session.Create("session-id", "csrf-token");
        session.SetTokens("token", "refresh-token", DateTime.UtcNow.AddSeconds(20), DateTime.UtcNow.AddDays(7));

        // Act
        var result = _service.RequiresRefresh(session);

        // Assert - Should be true because session expires in less than 30 seconds
        result.Should().BeTrue();
    }

    [Fact]
    public void RequiresRefresh_WithSessionExpiringIn20Minutes_ShouldReturnFalse()
    {
        // Arrange
        var session = Session.Create("session-id", "csrf-token");
        session.SetTokens("token", "refresh-token", DateTime.UtcNow.AddMinutes(20), DateTime.UtcNow.AddDays(7));

        // Act
        var result = _service.RequiresRefresh(session);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void RequiresRefresh_WithValidSessionButNoRefreshToken_ShouldReturnFalse()
    {
        // Arrange
        var session = Session.Create("session-id", "csrf-token");
        session.SetTokens("token", "", DateTime.UtcNow.AddMinutes(10), DateTime.UtcNow.AddDays(7));

        // Act
        var result = _service.RequiresRefresh(session);

        // Assert
        result.Should().BeFalse();
    }
}
