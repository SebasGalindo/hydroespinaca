using AuthService.Application.Features.Users.Commands.CreateUser;

namespace AuthService.Application.Tests.Features.Users;

public class CreateUserCommandValidatorTests
{
    private readonly CreateUserCommandValidator _validator;

    public CreateUserCommandValidatorTests()
    {
        _validator = new CreateUserCommandValidator();
    }

    [Fact]
    public void Validate_ValidCommand_ShouldPass()
    {
        // Arrange
        var command = new CreateUserCommand(
            "testuser",
            "test@example.com",
            "Password123!",
            "507f1f77bcf86cd799439011"); // Valid 24-character ObjectId

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
    public void Validate_EmptyUsername_ShouldFail(string username)
    {
        // Arrange
        var command = new CreateUserCommand(
            username,
            "test@example.com",
            "Password123!",
            null);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Username");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Validate_EmptyEmail_ShouldFail(string email)
    {
        // Arrange
        var command = new CreateUserCommand(
            "testuser",
            email,
            "Password123!",
            null);

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
        var command = new CreateUserCommand(
            "testuser",
            email,
            "Password123!",
            null);

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
        var command = new CreateUserCommand(
            "testuser",
            "test@example.com",
            password,
            null);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Password");
    }

    [Theory]
    [InlineData("short")]
    [InlineData("1234567")]
    public void Validate_PasswordTooShort_ShouldFail(string password)
    {
        // Arrange
        var command = new CreateUserCommand(
            "testuser",
            "test@example.com",
            password,
            null);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Password");
    }
}
