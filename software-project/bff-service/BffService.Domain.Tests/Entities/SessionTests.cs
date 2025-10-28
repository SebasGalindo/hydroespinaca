using BffService.Domain.Entities;

namespace BffService.Domain.Tests.Entities;

public class SessionTests
{
    [Fact]
    public void Create_ShouldInitializeSessionWithCorrectValues()
    {
        // Arrange
        var sessionId = "test-session-id";
        var csrfToken = "test-csrf-token";

        // Act
        var session = Session.Create(sessionId, csrfToken);

        // Assert
        session.SessionId.Should().Be(sessionId);
        session.CsrfToken.Should().Be(csrfToken);
        session.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        session.Scopes.Should().NotBeNull();
        session.Scopes.Should().BeEmpty();
    }

    [Fact]
    public void SetTokens_ShouldSetTokensAndExpirationDates()
    {
        // Arrange
        var session = Session.Create("session-id", "csrf-token");
        var accessToken = "access-token";
        var refreshToken = "refresh-token";
        var expiresAt = DateTime.UtcNow.AddHours(1);
        var refreshTokenExpiresAt = DateTime.UtcNow.AddDays(7);

        // Act
        session.SetTokens(accessToken, refreshToken, expiresAt, refreshTokenExpiresAt);

        // Assert
        session.AccessToken.Should().Be(accessToken);
        session.RefreshToken.Should().Be(refreshToken);
        session.ExpiresAt.Should().Be(expiresAt);
        session.RefreshTokenExpiresAt.Should().Be(refreshTokenExpiresAt);
    }

    [Fact]
    public void SetUserInfo_ShouldSetAllUserInformation()
    {
        // Arrange
        var session = Session.Create("session-id", "csrf-token");
        var userId = "user-123";
        var username = "testuser";
        var email = "test@example.com";
        var userRole = "admin";
        var scopes = new List<string> { "read", "write" };

        // Act
        session.SetUserInfo(userId, username, email, userRole, scopes);

        // Assert
        session.UserId.Should().Be(userId);
        session.Username.Should().Be(username);
        session.Email.Should().Be(email);
        session.UserRole.Should().Be(userRole);
        session.Scopes.Should().BeEquivalentTo(scopes);
    }

    [Fact]
    public void SetUserInfo_WithNullScopes_ShouldInitializeEmptyList()
    {
        // Arrange
        var session = Session.Create("session-id", "csrf-token");

        // Act
        session.SetUserInfo("user-123", "testuser", "test@example.com", "user", null);

        // Assert
        session.Scopes.Should().NotBeNull();
        session.Scopes.Should().BeEmpty();
    }

    [Fact]
    public void UpdateAccessToken_ShouldUpdateTokenAndExpiration()
    {
        // Arrange
        var session = Session.Create("session-id", "csrf-token");
        session.SetTokens("old-token", "refresh-token", DateTime.UtcNow.AddHours(1));
        var newAccessToken = "new-access-token";
        var newExpiresAt = DateTime.UtcNow.AddHours(2);

        // Act
        session.UpdateAccessToken(newAccessToken, newExpiresAt);

        // Assert
        session.AccessToken.Should().Be(newAccessToken);
        session.ExpiresAt.Should().Be(newExpiresAt);
    }

    [Fact]
    public void IsExpired_WithExpiredSession_ShouldReturnTrue()
    {
        // Arrange
        var session = Session.Create("session-id", "csrf-token");
        session.SetTokens("token", "refresh", DateTime.UtcNow.AddSeconds(-1));

        // Act
        var result = session.IsExpired();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsExpired_WithValidSession_ShouldReturnFalse()
    {
        // Arrange
        var session = Session.Create("session-id", "csrf-token");
        session.SetTokens("token", "refresh", DateTime.UtcNow.AddHours(1));

        // Act
        var result = session.IsExpired();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsRefreshTokenExpired_WithExpiredRefreshToken_ShouldReturnTrue()
    {
        // Arrange
        var session = Session.Create("session-id", "csrf-token");
        session.SetTokens("token", "refresh", DateTime.UtcNow.AddHours(1), DateTime.UtcNow.AddSeconds(-1));

        // Act
        var result = session.IsRefreshTokenExpired();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsRefreshTokenExpired_WithValidRefreshToken_ShouldReturnFalse()
    {
        // Arrange
        var session = Session.Create("session-id", "csrf-token");
        session.SetTokens("token", "refresh", DateTime.UtcNow.AddHours(1), DateTime.UtcNow.AddDays(7));

        // Act
        var result = session.IsRefreshTokenExpired();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsRefreshTokenExpired_WithNullRefreshTokenExpiry_ShouldReturnFalse()
    {
        // Arrange
        var session = Session.Create("session-id", "csrf-token");
        session.SetTokens("token", "refresh", DateTime.UtcNow.AddHours(1), null);

        // Act
        var result = session.IsRefreshTokenExpired();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void HasValidTokens_WithValidTokens_ShouldReturnTrue()
    {
        // Arrange
        var session = Session.Create("session-id", "csrf-token");
        session.SetTokens("valid-token", "refresh", DateTime.UtcNow.AddHours(1));

        // Act
        var result = session.HasValidTokens();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void HasValidTokens_WithExpiredTokens_ShouldReturnFalse()
    {
        // Arrange
        var session = Session.Create("session-id", "csrf-token");
        session.SetTokens("token", "refresh", DateTime.UtcNow.AddSeconds(-1));

        // Act
        var result = session.HasValidTokens();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void CanRefresh_WithValidRefreshToken_ShouldReturnTrue()
    {
        // Arrange
        var session = Session.Create("session-id", "csrf-token");
        session.SetTokens("token", "valid-refresh-token", DateTime.UtcNow.AddHours(1), DateTime.UtcNow.AddDays(7));

        // Act
        var result = session.CanRefresh();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void CanRefresh_WithExpiredRefreshToken_ShouldReturnFalse()
    {
        // Arrange
        var session = Session.Create("session-id", "csrf-token");
        session.SetTokens("token", "refresh-token", DateTime.UtcNow.AddHours(1), DateTime.UtcNow.AddSeconds(-1));

        // Act
        var result = session.CanRefresh();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void CanRefresh_WithEmptyRefreshToken_ShouldReturnFalse()
    {
        // Arrange
        var session = Session.Create("session-id", "csrf-token");
        session.SetTokens("token", "", DateTime.UtcNow.AddHours(1), DateTime.UtcNow.AddDays(7));

        // Act
        var result = session.CanRefresh();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Invalidate_ShouldClearTokensAndSetExpiredTime()
    {
        // Arrange
        var session = Session.Create("session-id", "csrf-token");
        session.SetTokens("token", "refresh", DateTime.UtcNow.AddHours(1));

        // Act
        session.Invalidate();

        // Assert
        session.AccessToken.Should().BeEmpty();
        session.RefreshToken.Should().BeEmpty();
        session.ExpiresAt.Should().BeBefore(DateTime.UtcNow);
        session.IsExpired().Should().BeTrue();
    }
}
