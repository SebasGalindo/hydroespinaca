using AuthService.Domain.ValueObjects;

namespace AuthService.Domain.Tests.ValueObjects;

public class EmailTests
{
    [Fact]
    public void Constructor_ValidEmail_ShouldCreateEmailInstance()
    {
        // Arrange
        var validEmail = "test@example.com";

        // Act
        var email = new Email(validEmail);

        // Assert
        email.Should().NotBeNull();
        email.Value.Should().Be(validEmail);
    }

    [Theory]
    [InlineData("user@domain.com")]
    [InlineData("test.user@example.co.uk")]
    [InlineData("name+tag@company.org")]
    [InlineData("a@b.c")]
    public void Constructor_ValidEmailFormats_ShouldCreateEmailInstance(string validEmail)
    {
        // Act
        var email = new Email(validEmail);

        // Assert
        email.Should().NotBeNull();
        email.Value.Should().Be(validEmail);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Constructor_NullOrWhiteSpace_ShouldThrowArgumentException(string invalidEmail)
    {
        // Act
        Action act = () => new Email(invalidEmail);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("Email no puede estar vacío.");
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("@example.com")]
    [InlineData("user@")]
    [InlineData("user@domain")]
    [InlineData("user domain@example.com")]
    [InlineData("user@@example.com")]
    public void Constructor_InvalidEmailFormat_ShouldThrowArgumentException(string invalidEmail)
    {
        // Act
        Action act = () => new Email(invalidEmail);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("Email no tiene un formato válido.*");
    }

    [Fact]
    public void Equals_SameEmailValue_ShouldReturnTrue()
    {
        // Arrange
        var email1 = new Email("test@example.com");
        var email2 = new Email("test@example.com");

        // Act
        var result = email1.Equals(email2);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void Equals_DifferentEmailValue_ShouldReturnFalse()
    {
        // Arrange
        var email1 = new Email("test1@example.com");
        var email2 = new Email("test2@example.com");

        // Act
        var result = email1.Equals(email2);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Equals_NullObject_ShouldReturnFalse()
    {
        // Arrange
        var email = new Email("test@example.com");

        // Act
        var result = email.Equals(null);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void GetHashCode_SameEmailValue_ShouldReturnSameHashCode()
    {
        // Arrange
        var email1 = new Email("test@example.com");
        var email2 = new Email("test@example.com");

        // Act
        var hash1 = email1.GetHashCode();
        var hash2 = email2.GetHashCode();

        // Assert
        hash1.Should().Be(hash2);
    }

    [Fact]
    public void GetHashCode_DifferentEmailValue_ShouldReturnDifferentHashCode()
    {
        // Arrange
        var email1 = new Email("test1@example.com");
        var email2 = new Email("test2@example.com");

        // Act
        var hash1 = email1.GetHashCode();
        var hash2 = email2.GetHashCode();

        // Assert
        hash1.Should().NotBe(hash2);
    }
}
