using AuthService.Domain.ValueObjects;

namespace AuthService.Domain.Tests.ValueObjects;

public class RefreshTokenResultTests
{
    [Fact]
    public void Constructor_ValidParameters_ShouldCreateRefreshTokenResultInstance()
    {
        // Arrange
        var userId = "user123";
        var email = "test@example.com";
        var role = "Admin";
        var clientId = "client123";

        // Act
        var result = new RefreshTokenResult(userId, email, role, clientId);

        // Assert
        result.Should().NotBeNull();
        result.UserId.Should().Be(userId);
        result.Email.Should().Be(email);
        result.Role.Should().Be(role);
        result.ClientId.Should().Be(clientId);
    }

    [Fact]
    public void Equals_SameValues_ShouldReturnTrue()
    {
        // Arrange
        var result1 = new RefreshTokenResult("user123", "test@example.com", "Admin", "client123");
        var result2 = new RefreshTokenResult("user123", "test@example.com", "Admin", "client123");

        // Act & Assert
        result1.Should().Be(result2);
    }

    [Fact]
    public void Equals_DifferentValues_ShouldReturnFalse()
    {
        // Arrange
        var result1 = new RefreshTokenResult("user123", "test@example.com", "Admin", "client123");
        var result2 = new RefreshTokenResult("user456", "other@example.com", "User", "client456");

        // Act & Assert
        result1.Should().NotBe(result2);
    }

    [Fact]
    public void GetHashCode_SameValues_ShouldReturnSameHashCode()
    {
        // Arrange
        var result1 = new RefreshTokenResult("user123", "test@example.com", "Admin", "client123");
        var result2 = new RefreshTokenResult("user123", "test@example.com", "Admin", "client123");

        // Act & Assert
        result1.GetHashCode().Should().Be(result2.GetHashCode());
    }

    [Fact]
    public void ToString_ShouldReturnStringRepresentation()
    {
        // Arrange
        var result = new RefreshTokenResult("user123", "test@example.com", "Admin", "client123");

        // Act
        var toString = result.ToString();

        // Assert
        toString.Should().NotBeNullOrEmpty();
        toString.Should().Contain("user123");
        toString.Should().Contain("test@example.com");
        toString.Should().Contain("Admin");
        toString.Should().Contain("client123");
    }
}
