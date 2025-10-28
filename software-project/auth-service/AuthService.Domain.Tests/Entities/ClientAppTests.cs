using AuthService.Domain.Entities;
using AuthService.Domain.Interfaces;
using AuthService.Domain.ValueObjects;

namespace AuthService.Domain.Tests.Entities;

public class ClientAppTests
{
    private HashedPassword CreateValidPassword() => new HashedPassword("$2a$11$hashedSecret");

    [Fact]
    public void Constructor_ValidParameters_ShouldCreateClientAppInstance()
    {
        // Arrange
        var code = "TEST_CLIENT";
        var clientId = "test-client-id";
        var secret = CreateValidPassword();
        var scopes = new[] { "read:data", "write:data" };

        // Act
        var clientApp = new ClientApp(code, clientId, secret, scopes);

        // Assert
        clientApp.Should().NotBeNull();
        clientApp.Code.Should().Be(code);
        clientApp.ClientId.Should().Be(clientId);
        clientApp.Secret.Should().Be(secret);
        clientApp.Scopes.Should().BeEquivalentTo(scopes);
        clientApp.Id.Should().NotBeNullOrEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Constructor_NullOrEmptyCode_ShouldThrowArgumentException(string code)
    {
        // Arrange
        var clientId = "test-client-id";
        var secret = CreateValidPassword();
        var scopes = new[] { "read:data" };

        // Act
        Action act = () => new ClientApp(code, clientId, secret, scopes);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("Client code cannot be null or empty*");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Constructor_NullOrEmptyClientId_ShouldThrowArgumentException(string clientId)
    {
        // Arrange
        var code = "TEST_CLIENT";
        var secret = CreateValidPassword();
        var scopes = new[] { "read:data" };

        // Act
        Action act = () => new ClientApp(code, clientId, secret, scopes);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("Client ID cannot be null or empty*");
    }

    [Fact]
    public void Constructor_NullSecret_ShouldThrowArgumentNullException()
    {
        // Arrange
        var code = "TEST_CLIENT";
        var clientId = "test-client-id";
        var scopes = new[] { "read:data" };

        // Act
        Action act = () => new ClientApp(code, clientId, null!, scopes);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("secret");
    }

    [Fact]
    public void Constructor_NullScopes_ShouldCreateClientAppWithEmptyScopes()
    {
        // Arrange
        var code = "TEST_CLIENT";
        var clientId = "test-client-id";
        var secret = CreateValidPassword();

        // Act
        var clientApp = new ClientApp(code, clientId, secret, null!);

        // Assert
        clientApp.Scopes.Should().NotBeNull();
        clientApp.Scopes.Should().BeEmpty();
    }

    [Fact]
    public void VerifySecret_CorrectSecret_ShouldReturnTrue()
    {
        // Arrange
        var plainSecret = "my-secret";
        var clientApp = new ClientApp(
            "TEST_CLIENT",
            "test-client-id",
            CreateValidPassword(),
            new[] { "read:data" });

        var hasherMock = new Mock<IPasswordHasher>();
        hasherMock.Setup(x => x.Verify(It.IsAny<string>(), plainSecret))
            .Returns(true);

        // Act
        var result = clientApp.VerifySecret(plainSecret, hasherMock.Object);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void VerifySecret_IncorrectSecret_ShouldReturnFalse()
    {
        // Arrange
        var plainSecret = "wrong-secret";
        var clientApp = new ClientApp(
            "TEST_CLIENT",
            "test-client-id",
            CreateValidPassword(),
            new[] { "read:data" });

        var hasherMock = new Mock<IPasswordHasher>();
        hasherMock.Setup(x => x.Verify(It.IsAny<string>(), plainSecret))
            .Returns(false);

        // Act
        var result = clientApp.VerifySecret(plainSecret, hasherMock.Object);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void SetId_ValidId_ShouldSetClientAppId()
    {
        // Arrange
        var clientApp = new ClientApp(
            "TEST_CLIENT",
            "test-client-id",
            CreateValidPassword(),
            new[] { "read:data" });
        var newId = "new-id-123";

        // Act
        clientApp.SetId(newId);

        // Assert
        clientApp.Id.Should().Be(newId);
    }

    [Fact]
    public void Constructor_EmptyScopes_ShouldCreateClientAppWithEmptyScopes()
    {
        // Arrange
        var code = "TEST_CLIENT";
        var clientId = "test-client-id";
        var secret = CreateValidPassword();
        var scopes = Array.Empty<string>();

        // Act
        var clientApp = new ClientApp(code, clientId, secret, scopes);

        // Assert
        clientApp.Scopes.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_MultipleScopes_ShouldStoreAllScopes()
    {
        // Arrange
        var scopes = new[] { "read:sensors", "write:sensors", "read:actuators", "write:actuators" };
        var clientApp = new ClientApp(
            "TEST_CLIENT",
            "test-client-id",
            CreateValidPassword(),
            scopes);

        // Act & Assert
        clientApp.Scopes.Should().HaveCount(4);
        clientApp.Scopes.Should().Contain("read:sensors");
        clientApp.Scopes.Should().Contain("write:sensors");
        clientApp.Scopes.Should().Contain("read:actuators");
        clientApp.Scopes.Should().Contain("write:actuators");
    }
}
