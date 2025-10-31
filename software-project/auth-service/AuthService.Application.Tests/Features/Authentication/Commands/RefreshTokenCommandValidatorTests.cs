using AuthService.Application.Features.Authentication.Commands.RefreshToken;

namespace AuthService.Application.Tests.Features.Authentication.Commands;

public class RefreshTokenCommandValidatorTests
{
    private readonly RefreshTokenCommandValidator _validator;

    public RefreshTokenCommandValidatorTests()
    {
        _validator = new RefreshTokenCommandValidator();
    }

    [Fact]
    public void Validate_ValidCommand_ShouldPass()
    {
        // Arrange
        var command = new RefreshTokenCommand("valid-refresh-token-12345");

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
    public void Validate_EmptyRefreshToken_ShouldFail(string refreshToken)
    {
        // Arrange
        var command = new RefreshTokenCommand(refreshToken);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "RefreshToken");
    }
}
