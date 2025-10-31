using AuthService.Domain.Entities;

namespace AuthService.Domain.Tests.Entities;

public class RefreshTokenTests
{
    [Fact]
    public void Constructor_ValidParameters_ShouldCreateRefreshTokenInstance()
    {
        // Arrange
        var userId = "user123";
        var token = "refresh_token_value";
        var expiresAt = DateTime.UtcNow.AddDays(7);
        var clientId = "client123";

        // Act
        var refreshToken = new RefreshToken(userId, token, expiresAt, clientId);

        // Assert
        refreshToken.Should().NotBeNull();
        refreshToken.UserId.Should().Be(userId);
        refreshToken.Token.Should().Be(token);
        refreshToken.ExpiresAt.Should().Be(expiresAt);
        refreshToken.ClientId.Should().Be(clientId);
        refreshToken.Revoked.Should().BeFalse();
        refreshToken.Id.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void SetId_ValidId_ShouldSetRefreshTokenId()
    {
        // Arrange
        var refreshToken = new RefreshToken("user123", "token", DateTime.UtcNow.AddDays(7), "client123");
        var tokenId = "token123";

        // Act
        refreshToken.SetId(tokenId);

        // Assert
        refreshToken.Id.Should().Be(tokenId);
    }

    [Fact]
    public void Revoke_ShouldSetRevokedToTrue()
    {
        // Arrange
        var refreshToken = new RefreshToken("user123", "token", DateTime.UtcNow.AddDays(7), "client123");

        // Act
        refreshToken.Revoke();

        // Assert
        refreshToken.Revoked.Should().BeTrue();
    }

    [Fact]
    public void IsActive_NotRevokedAndNotExpired_ShouldReturnTrue()
    {
        // Arrange
        var refreshToken = new RefreshToken("user123", "token", DateTime.UtcNow.AddDays(7), "client123");

        // Act
        var isActive = refreshToken.IsActive;

        // Assert
        isActive.Should().BeTrue();
    }

    [Fact]
    public void IsActive_Revoked_ShouldReturnFalse()
    {
        // Arrange
        var refreshToken = new RefreshToken("user123", "token", DateTime.UtcNow.AddDays(7), "client123");
        refreshToken.Revoke();

        // Act
        var isActive = refreshToken.IsActive;

        // Assert
        isActive.Should().BeFalse();
    }

    [Fact]
    public void IsActive_Expired_ShouldReturnFalse()
    {
        // Arrange
        var refreshToken = new RefreshToken("user123", "token", DateTime.UtcNow.AddMinutes(-5), "client123");

        // Act
        var isActive = refreshToken.IsActive;

        // Assert
        isActive.Should().BeFalse();
    }

    [Fact]
    public void IsActive_RevokedAndExpired_ShouldReturnFalse()
    {
        // Arrange
        var refreshToken = new RefreshToken("user123", "token", DateTime.UtcNow.AddMinutes(-5), "client123");
        refreshToken.Revoke();

        // Act
        var isActive = refreshToken.IsActive;

        // Assert
        isActive.Should().BeFalse();
    }
}
