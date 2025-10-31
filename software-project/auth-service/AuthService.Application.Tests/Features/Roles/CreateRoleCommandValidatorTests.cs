using AuthService.Application.Features.Roles.Commands.CreateRole;

namespace AuthService.Application.Tests.Features.Roles;

public class CreateRoleCommandValidatorTests
{
    private readonly CreateRoleCommandValidator _validator;

    public CreateRoleCommandValidatorTests()
    {
        _validator = new CreateRoleCommandValidator();
    }

    [Fact]
    public void Validate_ValidCommand_ShouldPass()
    {
        // Arrange
        var command = new CreateRoleCommand(
            "role_admin",
            "Administrator",
            new List<string> { "user:read", "user:write", "actuator:control" }
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
        var command = new CreateRoleCommand(
            code,
            "Administrator",
            new List<string>()
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Code");
    }

    [Theory]
    [InlineData("admin")] // Doesn't start with role_
    [InlineData("ROLE_admin")] // Wrong case
    [InlineData("user_admin")] // Wrong prefix
    public void Validate_CodeNotStartingWithRolePrefix_ShouldFail(string code)
    {
        // Arrange
        var command = new CreateRoleCommand(
            code,
            "Administrator",
            new List<string>()
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Code" && e.ErrorMessage.Contains("role_"));
    }

    [Theory]
    [InlineData("role")] // Too short (4 chars)
    [InlineData("rol_")] // Too short but has underscore
    public void Validate_CodeTooShort_ShouldFail(string code)
    {
        // Arrange
        var command = new CreateRoleCommand(
            code,
            "Administrator",
            new List<string>()
        );

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
        var longCode = "role_" + new string('a', 96); // More than 100 chars
        var command = new CreateRoleCommand(
            longCode,
            "Administrator",
            new List<string>()
        );

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
        var command = new CreateRoleCommand(
            "role_admin",
            name,
            new List<string>()
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Theory]
    [InlineData("A")] // Too short (1 char)
    public void Validate_NameTooShort_ShouldFail(string name)
    {
        // Arrange
        var command = new CreateRoleCommand(
            "role_admin",
            name,
            new List<string>()
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Fact]
    public void Validate_NameTooLong_ShouldFail()
    {
        // Arrange
        var longName = new string('a', 101); // More than 100 chars
        var command = new CreateRoleCommand(
            "role_admin",
            longName,
            new List<string>()
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Fact]
    public void Validate_NullPermissionCodes_ShouldFail()
    {
        // Arrange
        var command = new CreateRoleCommand(
            "role_admin",
            "Administrator",
            null!
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "PermissionCodes");
    }

    [Theory]
    [InlineData("read")] // Missing colon
    [InlineData("userread")] // Missing colon
    [InlineData("USER_READ")] // Missing colon
    public void Validate_InvalidPermissionCodeFormat_ShouldFail(string permissionCode)
    {
        // Arrange
        var command = new CreateRoleCommand(
            "role_admin",
            "Administrator",
            new List<string> { permissionCode }
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.Contains("PermissionCodes"));
    }

    [Fact]
    public void Validate_EmptyPermissionCodes_ShouldPass()
    {
        // Arrange
        var command = new CreateRoleCommand(
            "role_admin",
            "Administrator",
            new List<string>()
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_ValidPermissionCodesFormat_ShouldPass()
    {
        // Arrange
        var command = new CreateRoleCommand(
            "role_admin",
            "Administrator",
            new List<string> { "user:read", "user:write", "actuator:control", "device:manage" }
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }
}
