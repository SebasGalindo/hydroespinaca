using AuthService.Application.Features.Users.Commands.DeleteUser;

namespace AuthService.Application.Tests.Features.Users;

public class DeleteUserCommandValidatorTests
{
    private readonly DeleteUserCommandValidator _validator;

    public DeleteUserCommandValidatorTests()
    {
        _validator = new DeleteUserCommandValidator();
    }

    [Fact]
    public void Validate_ValidId_ShouldPass()
    {
        // Arrange
        var command = new DeleteUserCommand("507f1f77bcf86cd799439011");

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
        var command = new DeleteUserCommand(id);

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
        var command = new DeleteUserCommand(id);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Id");
    }
}
