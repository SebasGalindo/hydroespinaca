using AuthService.Application.Features.Roles.Commands.UpdateRole;

namespace AuthService.Application.Tests.Features.Roles;

public class UpdateRoleCommandValidatorTests
{
    private readonly UpdateRoleCommandValidator _validator;

    public UpdateRoleCommandValidatorTests()
    {
        _validator = new UpdateRoleCommandValidator();
    }

    [Fact]
    public void Validate_ValidCommand_ShouldPass()
    {
        // Arrange
        var command = new UpdateRoleCommand(
            "507f1f77bcf86cd799439011",
            "Administrator",
            new List<string> { "user:read", "user:write" }
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
    public void Validate_EmptyIdOrCode_ShouldFail(string idOrCode)
    {
        // Arrange
        var command = new UpdateRoleCommand(
            idOrCode,
            "Administrator",
            new List<string>()
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "IdOrCode");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Validate_EmptyName_ShouldFail(string name)
    {
        // Arrange
        var command = new UpdateRoleCommand(
            "507f1f77bcf86cd799439011",
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
    [InlineData("A")] // Too short
    public void Validate_NameTooShort_ShouldFail(string name)
    {
        // Arrange
        var command = new UpdateRoleCommand(
            "507f1f77bcf86cd799439011",
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
        var longName = new string('a', 101);
        var command = new UpdateRoleCommand(
            "507f1f77bcf86cd799439011",
            longName,
            new List<string>()
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Theory]
    [InlineData("read")] // Missing colon
    [InlineData("userread")] // Missing colon
    public void Validate_InvalidPermissionCodeFormat_ShouldFail(string permissionCode)
    {
        // Arrange
        var command = new UpdateRoleCommand(
            "507f1f77bcf86cd799439011",
            "Administrator",
            new List<string> { permissionCode }
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.Contains("PermissionCodes"));
    }
}
