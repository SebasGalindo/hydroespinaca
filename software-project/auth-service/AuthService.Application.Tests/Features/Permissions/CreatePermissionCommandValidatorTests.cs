using AuthService.Application.Features.Permissions.Commands.CreatePermission;

namespace AuthService.Application.Tests.Features.Permissions;

public class CreatePermissionCommandValidatorTests
{
    private readonly CreatePermissionCommandValidator _validator;

    public CreatePermissionCommandValidatorTests()
    {
        _validator = new CreatePermissionCommandValidator();
    }

    [Fact]
    public void Validate_ValidCommand_ShouldPass()
    {
        // Arrange
        var command = new CreatePermissionCommand(
            "perm_user_read",
            "Read Users",
            "Allows reading user information"
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
    public void Validate_EmptyCode_ShouldFail(string code)
    {
        // Arrange
        var command = new CreatePermissionCommand(code, "Read Users");

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Code");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Validate_EmptyName_ShouldFail(string name)
    {
        // Arrange
        var command = new CreatePermissionCommand("perm_user_read", name);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Fact]
    public void Validate_WithoutDescription_ShouldPass()
    {
        // Arrange
        var command = new CreatePermissionCommand(
            "perm_user_read",
            "Read Users",
            null
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("read")] // Doesn't start with perm_
    [InlineData("userread")] // Doesn't start with perm_
    [InlineData("USER_READ")] // Doesn't start with perm_
    [InlineData("user:read")] // Wrong format
    public void Validate_InvalidCodeFormat_ShouldFail(string code)
    {
        // Arrange
        var command = new CreatePermissionCommand(code, "Read Users");

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Code");
    }

    [Fact]
    public void Validate_CodeTooShort_ShouldFail()
    {
        // Arrange
        var command = new CreatePermissionCommand("perm", "Test Permission"); // Only 4 chars

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Code");
    }

    [Fact]
    public void Validate_CodeTooLong_ShouldFail()
    {
        // Arrange
        var longCode = "perm_" + new string('a', 96); // More than 100 chars
        var command = new CreatePermissionCommand(longCode, "Test Permission");

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Code");
    }

    [Fact]
    public void Validate_ValidCodeFormats_ShouldPass()
    {
        // Arrange
        var validCodes = new[]
        {
            "perm_user_read",
            "perm_user_write",
            "perm_actuator_control",
            "perm_device_manage",
            "perm_sensor_read"
        };

        // Act & Assert
        foreach (var code in validCodes)
        {
            var command = new CreatePermissionCommand(code, "Test Permission");

            var result = _validator.Validate(command);
            result.IsValid.Should().BeTrue($"Code '{code}' should be valid");
        }
    }

    [Fact]
    public void Validate_DescriptionTooLong_ShouldFail()
    {
        // Arrange
        var longDescription = new string('a', 501); // More than 500 chars
        var command = new CreatePermissionCommand(
            "perm_user_read",
            "Test Permission",
            longDescription
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Description");
    }
}
