using AuthService.Application.Features.Roles.Commands.DeleteRole;

namespace AuthService.Application.Tests.Features.Roles.Commands;

public class DeleteRoleCommandValidatorTests
{
    private readonly DeleteRoleCommandValidator _validator;

    public DeleteRoleCommandValidatorTests()
    {
        _validator = new DeleteRoleCommandValidator();
    }

    [Fact]
    public void Validate_ValidCommandWithId_ShouldPass()
    {
        // Arrange
        var command = new DeleteRoleCommand("507f1f77bcf86cd799439011");

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
        var command = new DeleteRoleCommand("ADMIN");

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
        var command = new DeleteRoleCommand(idOrCode);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "IdOrCode");
    }
}
