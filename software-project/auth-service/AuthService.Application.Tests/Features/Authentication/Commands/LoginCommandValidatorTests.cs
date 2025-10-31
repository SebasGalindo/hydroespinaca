using AuthService.Application.Features.Authentication.Commands.Login;

namespace AuthService.Application.Tests.Features.Authentication.Commands;

public class LoginCommandValidatorTests
{
    private readonly LoginCommandValidator _validator;

    public LoginCommandValidatorTests()
    {
        _validator = new LoginCommandValidator();
    }

    [Fact]
    public void Validate_ValidCommand_ShouldPass()
    {
        // Arrange
        var command = new LoginCommand("test@example.com", "Password123!");

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Validate_EmptyEmail_ShouldFail(string email)
    {
        // Arrange
        var command = new LoginCommand(email, "Password123!");

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }

    [Theory]
    [InlineData("invalid-email")]
    [InlineData("@example.com")]
    [InlineData("user@")]
    [InlineData("user.example.com")]
    public void Validate_InvalidEmailFormat_ShouldFail(string email)
    {
        // Arrange
        var command = new LoginCommand(email, "Password123!");

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }

    [Fact]
    public void Validate_EmailTooLong_ShouldFail()
    {
        // Arrange
        var longEmail = new string('a', 244) + "@example.com"; // 256 characters
        var command = new LoginCommand(longEmail, "Password123!");

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Validate_EmptyPassword_ShouldFail(string password)
    {
        // Arrange
        var command = new LoginCommand("test@example.com", password);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Password");
    }

    [Theory]
    [InlineData("short")]
    [InlineData("12345")]
    public void Validate_PasswordTooShort_ShouldFail(string password)
    {
        // Arrange
        var command = new LoginCommand("test@example.com", password);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Password");
    }

    [Fact]
    public void Validate_PasswordTooLong_ShouldFail()
    {
        // Arrange
        var longPassword = new string('a', 101); // 101 characters
        var command = new LoginCommand("test@example.com", longPassword);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Password");
    }
}
