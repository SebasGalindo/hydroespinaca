using AuthService.Application.Features.Permissions.Commands.UpdatePermission;

namespace AuthService.Application.Tests.Features.Permissions;

public class UpdatePermissionCommandValidatorTests
{
    private readonly UpdatePermissionCommandValidator _validator;

    public UpdatePermissionCommandValidatorTests()
    {
        _validator = new UpdatePermissionCommandValidator();
    }

    [Fact]
    public void Validate_ValidCommand_ShouldPass()
    {
        // Arrange
        var command = new UpdatePermissionCommand(
            "507f1f77bcf86cd799439011",
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
    public void Validate_EmptyIdOrCode_ShouldFail(string idOrCode)
    {
        // Arrange
        var command = new UpdatePermissionCommand(idOrCode, "Read Users");

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
        var command = new UpdatePermissionCommand(
            "507f1f77bcf86cd799439011",
            name
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
        var command = new UpdatePermissionCommand(
            "507f1f77bcf86cd799439011",
            name
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
        var command = new UpdatePermissionCommand(
            "507f1f77bcf86cd799439011",
            longName
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Fact]
    public void Validate_DescriptionTooLong_ShouldFail()
    {
        // Arrange
        var longDescription = new string('a', 501);
        var command = new UpdatePermissionCommand(
            "507f1f77bcf86cd799439011",
            "Read Users",
            longDescription
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Description");
    }
}
