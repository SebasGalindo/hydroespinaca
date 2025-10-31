using AuthService.Application.Features.Authentication.Commands.ChangePassword;

namespace AuthService.Application.Tests.Features.Authentication.Commands;

public class ChangePasswordCommandValidatorTests
{
    private readonly ChangePasswordCommandValidator _validator;

    public ChangePasswordCommandValidatorTests()
    {
        _validator = new ChangePasswordCommandValidator();
    }

    [Fact]
    public void Validate_ValidCommand_ShouldPass()
    {
        // Arrange
        var command = new ChangePasswordCommand
        {
            UserId = "507f1f77bcf86cd799439011",
            OldPassword = "OldPassword123!",
            NewPassword = "NewPassword456@"
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
    public void Validate_EmptyUserId_ShouldFail(string userId)
    {
        // Arrange
        var command = new ChangePasswordCommand
        {
            UserId = userId,
            OldPassword = "OldPassword123!",
            NewPassword = "NewPassword456@"
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "UserId");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Validate_EmptyOldPassword_ShouldFail(string oldPassword)
    {
        // Arrange
        var command = new ChangePasswordCommand
        {
            UserId = "507f1f77bcf86cd799439011",
            OldPassword = oldPassword,
            NewPassword = "NewPassword456@"
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "OldPassword");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Validate_EmptyNewPassword_ShouldFail(string newPassword)
    {
        // Arrange
        var command = new ChangePasswordCommand
        {
            UserId = "507f1f77bcf86cd799439011",
            OldPassword = "OldPassword123!",
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
        var command = new ChangePasswordCommand
        {
            UserId = "507f1f77bcf86cd799439011",
            OldPassword = "OldPassword123!",
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
        var command = new ChangePasswordCommand
        {
            UserId = "507f1f77bcf86cd799439011",
            OldPassword = "OldPassword123!",
            NewPassword = newPassword
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "NewPassword");
    }

    [Fact]
    public void Validate_NewPasswordSameAsOld_ShouldFail()
    {
        // Arrange
        var samePassword = "Password123!";
        var command = new ChangePasswordCommand
        {
            UserId = "507f1f77bcf86cd799439011",
            OldPassword = samePassword,
            NewPassword = samePassword
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "NewPassword");
    }
}
