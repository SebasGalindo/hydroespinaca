using AuthService.Application.Exceptions;
using AuthService.Application.Features.Authentication.Commands.ClientCredentials;
using AuthService.Domain.Entities;
using AuthService.Domain.Enums;
using AuthService.Domain.Interfaces;
using AuthService.Domain.ValueObjects;
using HydroEspinaca.Shared.DTOs.Authentication;

namespace AuthService.Application.Tests.Features.Authentication;

public class ClientCredentialsCommandHandlerTests
{
    private readonly Mock<IClientAppRepository> _clientAppRepositoryMock;
    private readonly Mock<IPasswordHasher> _passwordHasherMock;
    private readonly Mock<ITokenService> _tokenServiceMock;
    private readonly ClientCredentialsCommandHandler _handler;

    public ClientCredentialsCommandHandlerTests()
    {
        _clientAppRepositoryMock = new Mock<IClientAppRepository>();
        _passwordHasherMock = new Mock<IPasswordHasher>();
        _tokenServiceMock = new Mock<ITokenService>();

        _handler = new ClientCredentialsCommandHandler(
            _clientAppRepositoryMock.Object,
            _passwordHasherMock.Object,
            _tokenServiceMock.Object);
    }

    [Fact]
    public async Task Handle_ValidCredentials_ShouldReturnTokenResult()
    {
        // Arrange
        var clientId = "test-client";
        var clientSecret = "test-secret";
        var command = new ClientCredentialsCommand(clientId, clientSecret);

        var clientApp = new ClientApp(
            "Test Client",
            "TEST_CLIENT",
            new HashedPassword("$2a$11$hashedSecret"),
            new[] { "read:data", "write:data" });
        clientApp.SetId("client123");

        _clientAppRepositoryMock
            .Setup(x => x.FindByClientIdAsync(clientId))
            .ReturnsAsync(clientApp);

        _passwordHasherMock
            .Setup(x => x.Verify(It.IsAny<string>(), clientSecret))
            .Returns(true);

        _tokenServiceMock
            .Setup(x => x.GenerateTokensAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<TokenType>(),
                It.IsAny<string[]>()))
            .ReturnsAsync(new TokenResult
            {
                AccessToken = "access-token",
                RefreshToken = "refresh-token",
                ExpiresAt = DateTime.UtcNow.AddHours(1),
                Role = HydroEspinaca.Shared.Constants.SystemRoles.Client,
                ClientId = "TEST_CLIENT",
                Scopes = new[] { "read:data", "write:data" }
            });

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.AccessToken.Should().Be("access-token");
        result.RefreshToken.Should().Be("refresh-token");
        result.ClientId.Should().Be("TEST_CLIENT");
        result.Scopes.Should().BeEquivalentTo(new[] { "read:data", "write:data" });

        _clientAppRepositoryMock.Verify(x => x.FindByClientIdAsync(clientId), Times.Once);
        _passwordHasherMock.Verify(x => x.Verify(It.IsAny<string>(), clientSecret), Times.Once);
        _tokenServiceMock.Verify(x => x.GenerateTokensAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            TokenType.MachineToMachine,
            It.IsAny<string[]>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ClientAppNotFound_ShouldThrowInvalidClientCredentialsException()
    {
        // Arrange
        var command = new ClientCredentialsCommand("non-existent-client", "secret");

        _clientAppRepositoryMock
            .Setup(x => x.FindByClientIdAsync(It.IsAny<string>()))
            .ReturnsAsync((ClientApp)null!);

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidClientCredentialsException>();
        _passwordHasherMock.Verify(x => x.Verify(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Handle_InvalidClientSecret_ShouldThrowInvalidClientCredentialsException()
    {
        // Arrange
        var command = new ClientCredentialsCommand("test-client", "wrong-secret");

        var clientApp = new ClientApp(
            "Test Client",
            "TEST_CLIENT",
            new HashedPassword("$2a$11$hashedSecret"),
            new[] { "read:data" });

        _clientAppRepositoryMock
            .Setup(x => x.FindByClientIdAsync(It.IsAny<string>()))
            .ReturnsAsync(clientApp);

        _passwordHasherMock
            .Setup(x => x.Verify(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(false);

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidClientCredentialsException>();
        _tokenServiceMock.Verify(x => x.GenerateTokensAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<TokenType>(),
            It.IsAny<string[]>()), Times.Never);
    }
}
