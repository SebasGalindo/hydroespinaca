using AuthService.Application.Features.Authentication.Commands.ForgotPassword;

namespace AuthService.Application.Tests.Features.Authentication.Commands;

public class ForgotPasswordCommandValidatorTests
{
    private readonly ForgotPasswordCommandValidator _validator;

    public ForgotPasswordCommandValidatorTests()
    {
        _validator = new ForgotPasswordCommandValidator();
    }

    [Fact]
    public void Validate_ValidCommand_ShouldPass()
    {
        // Arrange
        var command = new ForgotPasswordCommand { Email = "test@example.com" };

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
        var command = new ForgotPasswordCommand { Email = email };

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
        var command = new ForgotPasswordCommand { Email = email };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }
}
