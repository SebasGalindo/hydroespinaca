using AuthService.Application.Features.Authentication.Commands.ResetPassword;

namespace AuthService.Application.Tests.Features.Authentication.Commands;

public class ResetPasswordCommandValidatorTests
{
    private readonly ResetPasswordCommandValidator _validator;

    public ResetPasswordCommandValidatorTests()
    {
        _validator = new ResetPasswordCommandValidator();
    }

    [Fact]
    public void Validate_ValidCommand_ShouldPass()
    {
        // Arrange
        var command = new ResetPasswordCommand
        {
            Email = "test@example.com",
            Code = "ABC123",
            NewPassword = "NewPassword123!"
        };

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
        var command = new ResetPasswordCommand
        {
            Email = email,
            Code = "ABC123",
            NewPassword = "NewPassword123!"
        };

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
    public void Validate_InvalidEmailFormat_ShouldFail(string email)
    {
        // Arrange
        var command = new ResetPasswordCommand
        {
            Email = email,
            Code = "ABC123",
            NewPassword = "NewPassword123!"
        };

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
    public void Validate_EmptyCode_ShouldFail(string code)
    {
        // Arrange
        var command = new ResetPasswordCommand
        {
            Email = "test@example.com",
            Code = code,
            NewPassword = "NewPassword123!"
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Code");
    }

    [Theory]
    [InlineData("ABC12")]    // Too short
    [InlineData("ABC1234")]  // Too long
    public void Validate_CodeWrongLength_ShouldFail(string code)
    {
        // Arrange
        var command = new ResetPasswordCommand
        {
            Email = "test@example.com",
            Code = code,
            NewPassword = "NewPassword123!"
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Code");
    }

    [Theory]
    [InlineData("abc123")]   // Lowercase letters
    [InlineData("ABC12!")]   // Special character
    [InlineData("AB C12")]   // Space
    public void Validate_CodeInvalidFormat_ShouldFail(string code)
    {
        // Arrange
        var command = new ResetPasswordCommand
        {
            Email = "test@example.com",
            Code = code,
            NewPassword = "NewPassword123!"
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Code");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Validate_EmptyNewPassword_ShouldFail(string newPassword)
    {
        // Arrange
        var command = new ResetPasswordCommand
        {
            Email = "test@example.com",
            Code = "ABC123",
            NewPassword = newPassword
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "NewPassword");
    }

    [Theory]
    [InlineData("Short1!")]
    [InlineData("Pass1@")]
    public void Validate_NewPasswordTooShort_ShouldFail(string newPassword)
    {
        // Arrange
        var command = new ResetPasswordCommand
        {
            Email = "test@example.com",
            Code = "ABC123",
            NewPassword = newPassword
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "NewPassword");
    }

    [Theory]
    [InlineData("password123!")]  // No uppercase
    [InlineData("PASSWORD123!")]  // No lowercase
    [InlineData("Password!!!!")]  // No number
    [InlineData("Password1234")]  // No special char
    public void Validate_NewPasswordDoesNotMeetComplexity_ShouldFail(string newPassword)
    {
        // Arrange
        var command = new ResetPasswordCommand
        {
            Email = "test@example.com",
            Code = "ABC123",
            NewPassword = newPassword
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "NewPassword");
    }
}
