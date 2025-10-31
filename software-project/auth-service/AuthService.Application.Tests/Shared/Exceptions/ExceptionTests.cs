using AuthService.Application.Exceptions;

namespace AuthService.Application.Tests.Shared.Exceptions;

public class ExceptionTests
{
    [Fact]
    public void ClientAppAlreadyExistsException_WithClientId_ShouldContainClientIdInMessage()
    {
        // Arrange
        var clientId = "client-123";

        // Act
        var exception = new ClientAppAlreadyExistsException(clientId);

        // Assert
        exception.Should().NotBeNull();
        exception.Message.Should().Contain(clientId);
    }

    [Fact]
    public void InvalidRefreshTokenException_ShouldHaveDefaultMessage()
    {
        // Act
        var exception = new InvalidRefreshTokenException();

        // Assert
        exception.Should().NotBeNull();
        exception.Message.Should().NotBeNullOrEmpty();
        exception.Message.Should().Contain("invalid or expired");
    }
}
