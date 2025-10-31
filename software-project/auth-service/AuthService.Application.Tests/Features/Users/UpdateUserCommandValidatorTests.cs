using AuthService.Application.Features.Users.Commands.UpdateUser;

namespace AuthService.Application.Tests.Features.Users;

public class UpdateUserCommandValidatorTests
{
    private readonly UpdateUserCommandValidator _validator;

    public UpdateUserCommandValidatorTests()
    {
        _validator = new UpdateUserCommandValidator();
    }

    [Fact]
    public void Validate_ValidCommand_ShouldPass()
    {
        // Arrange
        var command = new UpdateUserCommand(
            "507f1f77bcf86cd799439011", // Valid 24-character ObjectId
            "newusername",
            "newemail@example.com",
            "NewPassword123!",
            "507f1f77bcf86cd799439012"
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Validate_EmptyId_ShouldFail(string id)
    {
        // Arrange
        var command = new UpdateUserCommand(id, "testuser");

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Id");
    }

    [Theory]
    [InlineData("123")] // Too short
    [InlineData("507f1f77bcf86cd79943901112345")] // Too long
    public void Validate_InvalidIdLength_ShouldFail(string id)
    {
        // Arrange
        var command = new UpdateUserCommand(id, "testuser");

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Id");
    }

    [Theory]
    [InlineData("invalid-email")]
    [InlineData("@example.com")]
    [InlineData("user@")]
    public void Validate_InvalidEmailFormat_ShouldFail(string email)
    {
        // Arrange
        var command = new UpdateUserCommand(
            "507f1f77bcf86cd799439011",
            "testuser",
            email
        );

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
        var longEmail = new string('a', 250) + "@example.com"; // More than 255 chars
        var command = new UpdateUserCommand(
            "507f1f77bcf86cd799439011",
            "testuser",
            longEmail
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }

    [Theory]
    [InlineData("short")]
    [InlineData("12345")]
    public void Validate_PasswordTooShort_ShouldFail(string password)
    {
        // Arrange
        var command = new UpdateUserCommand(
            "507f1f77bcf86cd799439011",
            "testuser",
            null,
            password
        );

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
        var longPassword = new string('a', 101); // More than 100 chars
        var command = new UpdateUserCommand(
            "507f1f77bcf86cd799439011",
            "testuser",
            null,
            longPassword
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Password");
    }

    [Theory]
    [InlineData("123")] // Too short
    [InlineData("507f1f77bcf86cd79943901112345")] // Too long
    public void Validate_InvalidRoleIdLength_ShouldFail(string roleId)
    {
        // Arrange
        var command = new UpdateUserCommand(
            "507f1f77bcf86cd799439011",
            "testuser",
            null,
            null,
            roleId
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "RoleId");
    }

    [Fact]
    public void Validate_NullOptionalFields_ShouldPass()
    {
        // Arrange
        var command = new UpdateUserCommand(
            "507f1f77bcf86cd799439011",
            "testuser",
            null,
            null,
            null
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }
}
