using AuthService.Application.Features.Permissions.Commands.DeletePermission;

namespace AuthService.Application.Tests.Features.Permissions.Commands;

public class DeletePermissionCommandValidatorTests
{
    private readonly DeletePermissionCommandValidator _validator;

    public DeletePermissionCommandValidatorTests()
    {
        _validator = new DeletePermissionCommandValidator();
    }

    [Fact]
    public void Validate_ValidCommandWithId_ShouldPass()
    {
        // Arrange
        var command = new DeletePermissionCommand("507f1f77bcf86cd799439011");

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_ValidCommandWithCode_ShouldPass()
    {
        // Arrange
        var command = new DeletePermissionCommand("PERM_CODE");

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
    public void Validate_EmptyIdOrCode_ShouldFail(string idOrCode)
    {
        // Arrange
        var command = new DeletePermissionCommand(idOrCode);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "IdOrCode");
    }
}
