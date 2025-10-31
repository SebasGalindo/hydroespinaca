using AuthService.Application.Features.Authentication.Commands.ClientCredentials;

namespace AuthService.Application.Tests.Features.Authentication.Commands;

public class ClientCredentialsCommandValidatorTests
{
    private readonly ClientCredentialsCommandValidator _validator;

    public ClientCredentialsCommandValidatorTests()
    {
        _validator = new ClientCredentialsCommandValidator();
    }

    [Fact]
    public void Validate_ValidCommand_ShouldPass()
    {
        // Arrange
        var command = new ClientCredentialsCommand("client-id-123", "client-secret-456");

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
    public void Validate_EmptyClientId_ShouldFail(string clientId)
    {
        // Arrange
        var command = new ClientCredentialsCommand(clientId, "client-secret-456");

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ClientId");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Validate_EmptyClientSecret_ShouldFail(string clientSecret)
    {
        // Arrange
        var command = new ClientCredentialsCommand("client-id-123", clientSecret);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ClientSecret");
    }

    [Fact]
    public void Validate_BothEmpty_ShouldFailWithMultipleErrors()
    {
        // Arrange
        var command = new ClientCredentialsCommand("", "");

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCount(2);
        result.Errors.Should().Contain(e => e.PropertyName == "ClientId");
        result.Errors.Should().Contain(e => e.PropertyName == "ClientSecret");
    }
}
