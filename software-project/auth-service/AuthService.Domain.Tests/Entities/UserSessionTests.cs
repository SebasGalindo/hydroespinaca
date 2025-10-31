using AuthService.Domain.Entities;

namespace AuthService.Domain.Tests.Entities;

public class UserSessionTests
{
    [Fact]
    public void Constructor_ValidParameters_ShouldCreateUserSessionInstance()
    {
        // Arrange
        var userId = "user123";
        var clientId = "client123";
        var sessionId = "session123";
        var refreshToken = "refresh_token";
        var accessTokenHash = "access_token_hash";
        var expiresAt = DateTime.UtcNow.AddHours(1);
        var ipAddress = "192.168.1.1";
        var userAgent = "Mozilla/5.0";
        var csrfToken = "csrf_token";

        // Act
        var userSession = new UserSession(
            userId,
            clientId,
            sessionId,
            refreshToken,
            accessTokenHash,
            expiresAt,
            ipAddress,
            userAgent,
            csrfToken);

        // Assert
        userSession.Should().NotBeNull();
        userSession.UserId.Should().Be(userId);
        userSession.ClientId.Should().Be(clientId);
        userSession.SessionId.Should().Be(sessionId);
        userSession.RefreshToken.Should().Be(refreshToken);
        userSession.AccessTokenHash.Should().Be(accessTokenHash);
        userSession.ExpiresAt.Should().Be(expiresAt);
        userSession.IpAddress.Should().Be(ipAddress);
        userSession.UserAgent.Should().Be(userAgent);
        userSession.CsrfToken.Should().Be(csrfToken);
        userSession.Revoked.Should().BeFalse();
        userSession.Id.Should().NotBeNullOrEmpty();
        userSession.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        userSession.LastActivity.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Constructor_WithoutOptionalParameters_ShouldCreateUserSessionWithNullValues()
    {
        // Arrange
        var userId = "user123";
        var clientId = "client123";
        var sessionId = "session123";
        var refreshToken = "refresh_token";
        var accessTokenHash = "access_token_hash";
        var expiresAt = DateTime.UtcNow.AddHours(1);

        // Act
        var userSession = new UserSession(
            userId,
            clientId,
            sessionId,
            refreshToken,
            accessTokenHash,
            expiresAt);

        // Assert
        userSession.IpAddress.Should().BeNull();
        userSession.UserAgent.Should().BeNull();
        userSession.CsrfToken.Should().BeNull();
    }

    [Fact]
    public void SetId_ValidId_ShouldSetUserSessionId()
    {
        // Arrange
        var userSession = new UserSession(
            "user123",
            "client123",
            "session123",
            "refresh_token",
            "access_token_hash",
            DateTime.UtcNow.AddHours(1));
        var sessionId = "session123";

        // Act
        userSession.SetId(sessionId);

        // Assert
        userSession.Id.Should().Be(sessionId);
    }

    [Fact]
    public void RestoreTimestamps_ValidTimestamps_ShouldUpdateTimestamps()
    {
        // Arrange
        var userSession = new UserSession(
            "user123",
            "client123",
            "session123",
            "refresh_token",
            "access_token_hash",
            DateTime.UtcNow.AddHours(1));
        var createdAt = DateTime.UtcNow.AddHours(-2);
        var lastActivity = DateTime.UtcNow.AddMinutes(-5);

        // Act
        userSession.RestoreTimestamps(createdAt, lastActivity);

        // Assert
        userSession.CreatedAt.Should().Be(createdAt);
        userSession.LastActivity.Should().Be(lastActivity);
    }

    [Fact]
    public void UpdateTokens_ValidTokens_ShouldUpdateTokensAndLastActivity()
    {
        // Arrange
        var userSession = new UserSession(
            "user123",
            "client123",
            "session123",
            "old_refresh_token",
            "old_access_token_hash",
            DateTime.UtcNow.AddHours(1));
        var oldLastActivity = userSession.LastActivity;
        var newRefreshToken = "new_refresh_token";
        var newAccessTokenHash = "new_access_token_hash";

        // Act
        Thread.Sleep(100); // Ensure time difference
        userSession.UpdateTokens(newRefreshToken, newAccessTokenHash);

        // Assert
        userSession.RefreshToken.Should().Be(newRefreshToken);
        userSession.AccessTokenHash.Should().Be(newAccessTokenHash);
        userSession.LastActivity.Should().BeAfter(oldLastActivity);
    }

    [Fact]
    public void UpdateActivity_ShouldUpdateLastActivity()
    {
        // Arrange
        var userSession = new UserSession(
            "user123",
            "client123",
            "session123",
            "refresh_token",
            "access_token_hash",
            DateTime.UtcNow.AddHours(1));
        var oldLastActivity = userSession.LastActivity;

        // Act
        Thread.Sleep(100); // Ensure time difference
        userSession.UpdateActivity();

        // Assert
        userSession.LastActivity.Should().BeAfter(oldLastActivity);
    }

    [Fact]
    public void Revoke_ShouldSetRevokedToTrueAndSetRevokedAt()
    {
        // Arrange
        var userSession = new UserSession(
            "user123",
            "client123",
            "session123",
            "refresh_token",
            "access_token_hash",
            DateTime.UtcNow.AddHours(1));

        // Act
        userSession.Revoke();

        // Assert
        userSession.Revoked.Should().BeTrue();
        userSession.RevokedAt.Should().NotBeNull();
        userSession.RevokedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void IsActive_NotRevokedAndNotExpired_ShouldReturnTrue()
    {
        // Arrange
        var userSession = new UserSession(
            "user123",
            "client123",
            "session123",
            "refresh_token",
            "access_token_hash",
            DateTime.UtcNow.AddHours(1));

        // Act
        var isActive = userSession.IsActive;

        // Assert
        isActive.Should().BeTrue();
    }

    [Fact]
    public void IsActive_Revoked_ShouldReturnFalse()
    {
        // Arrange
        var userSession = new UserSession(
            "user123",
            "client123",
            "session123",
            "refresh_token",
            "access_token_hash",
            DateTime.UtcNow.AddHours(1));
        userSession.Revoke();

        // Act
        var isActive = userSession.IsActive;

        // Assert
        isActive.Should().BeFalse();
    }

    [Fact]
    public void IsActive_Expired_ShouldReturnFalse()
    {
        // Arrange
        var userSession = new UserSession(
            "user123",
            "client123",
            "session123",
            "refresh_token",
            "access_token_hash",
            DateTime.UtcNow.AddMinutes(-5));

        // Act
        var isActive = userSession.IsActive;

        // Assert
        isActive.Should().BeFalse();
    }

    [Fact]
    public void IsActive_RevokedAndExpired_ShouldReturnFalse()
    {
        // Arrange
        var userSession = new UserSession(
            "user123",
            "client123",
            "session123",
            "refresh_token",
            "access_token_hash",
            DateTime.UtcNow.AddMinutes(-5));
        userSession.Revoke();

        // Act
        var isActive = userSession.IsActive;

        // Assert
        isActive.Should().BeFalse();
    }
}
