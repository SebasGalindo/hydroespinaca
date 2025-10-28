using AuthService.Domain.ValueObjects;

namespace AuthService.Domain.Tests.ValueObjects;

public class HashedPasswordTests
{
    [Fact]
    public void Constructor_ValidHash_ShouldCreateHashedPasswordInstance()
    {
        // Arrange
        var validHash = "$2a$11$abcdefghijklmnopqrstuvwxyz1234567890";

        // Act
        var hashedPassword = new HashedPassword(validHash);

        // Assert
        hashedPassword.Should().NotBeNull();
        hashedPassword.Value.Should().Be(validHash);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Constructor_NullOrWhiteSpace_ShouldThrowArgumentException(string invalidHash)
    {
        // Act
        Action act = () => new HashedPassword(invalidHash);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("El hash de la contraseña no puede estar vacío.");
    }

    [Fact]
    public void Equals_SameHashValue_ShouldReturnTrue()
    {
        // Arrange
        var hash = "$2a$11$abcdefghijklmnopqrstuvwxyz1234567890";
        var hashedPassword1 = new HashedPassword(hash);
        var hashedPassword2 = new HashedPassword(hash);

        // Act
        var result = hashedPassword1.Equals(hashedPassword2);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void Equals_DifferentHashValue_ShouldReturnFalse()
    {
        // Arrange
        var hashedPassword1 = new HashedPassword("$2a$11$hash1");
        var hashedPassword2 = new HashedPassword("$2a$11$hash2");

        // Act
        var result = hashedPassword1.Equals(hashedPassword2);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void GetHashCode_SameHashValue_ShouldReturnSameHashCode()
    {
        // Arrange
        var hash = "$2a$11$abcdefghijklmnopqrstuvwxyz1234567890";
        var hashedPassword1 = new HashedPassword(hash);
        var hashedPassword2 = new HashedPassword(hash);

        // Act
        var hashCode1 = hashedPassword1.GetHashCode();
        var hashCode2 = hashedPassword2.GetHashCode();

        // Assert
        hashCode1.Should().Be(hashCode2);
    }
}
