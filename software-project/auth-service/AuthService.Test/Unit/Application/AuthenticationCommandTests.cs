using AuthService.Application.Exceptions;
using AuthService.Application.Features.Authentication.Commands.Login;
using AuthService.Application.Features.Authentication.Commands.ClientCredentials;
using AuthService.Application.Features.Authentication.Commands.RefreshToken;
using AuthService.Domain.Entities;
using AuthService.Domain.Interfaces;
using AuthService.Domain.ValueObjects;
using AuthService.Test.Helpers;
using FluentAssertions;
using Moq;

namespace AuthService.Test.Unit.Application;

public class LoginCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IRoleRepository> _roleRepositoryMock;
    private readonly Mock<IPasswordHasher> _passwordHasherMock;
    private readonly Mock<ITokenService> _tokenServiceMock;
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepositoryMock;
    private readonly LoginCommandHandler _sut;

    public LoginCommandHandlerTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _roleRepositoryMock = new Mock<IRoleRepository>();
        _passwordHasherMock = new Mock<IPasswordHasher>();
        _tokenServiceMock = new Mock<ITokenService>();
        _refreshTokenRepositoryMock = new Mock<IRefreshTokenRepository>();
        _sut = new LoginCommandHandler(
            _userRepositoryMock.Object,
            _roleRepositoryMock.Object,
            _passwordHasherMock.Object,
            _tokenServiceMock.Object,
            _refreshTokenRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_ValidCredentials_ReturnsTokenResult()
    {
        // Arrange
        var command = new LoginCommand("test@example.com", "password123");
        var fakeUser = TestDataHelper.CreateTestUser(command.Email);

        _userRepositoryMock
            .Setup(r => r.FindByEmailAsync(command.Email))
            .ReturnsAsync(fakeUser);

        _passwordHasherMock
            .Setup(h => h.Verify(fakeUser.Password.Value, command.Password))
            .Returns(true);

        var expectedTokens = new TokenResult
        {
            AccessToken = "access_token",
            RefreshToken = "refresh_token",
            ExpiresAt = DateTime.UtcNow.AddMinutes(30),
            Role = "user",
            ClientId = null
        };

        _tokenServiceMock
            .Setup(s => s.GenerateTokens(It.IsAny<string>(), command.Email, It.IsAny<string>(), null))
            .Returns(expectedTokens);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.AccessToken.Should().Be(expectedTokens.AccessToken);
        result.RefreshToken.Should().Be(expectedTokens.RefreshToken);

        _userRepositoryMock.Verify(r => r.FindByEmailAsync(command.Email), Times.Once);
        _passwordHasherMock.Verify(h => h.Verify(fakeUser.Password.Value, command.Password), Times.Once);
        _refreshTokenRepositoryMock.Verify(r => r.AddAsync(It.IsAny<RefreshToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_InvalidCredentials_ThrowsInvalidCredentialsException()
    {
        // Arrange
        var command = new LoginCommand("invalid@example.com", "wrongpassword");

        _userRepositoryMock
            .Setup(r => r.FindByEmailAsync(command.Email))
            .ReturnsAsync((User?)null);

        // Act & Assert
        var act = async () => await _sut.Handle(command, CancellationToken.None);
        await act.Should().ThrowAsync<InvalidCredentialsException>();

        _userRepositoryMock.Verify(r => r.FindByEmailAsync(command.Email), Times.Once);
        _tokenServiceMock.Verify(s => s.GenerateTokens(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }
}

public class ClientCredentialsCommandHandlerTests
{
    private readonly Mock<IClientAppRepository> _clientAppRepositoryMock;
    private readonly Mock<IPasswordHasher> _passwordHasherMock;
    private readonly Mock<ITokenService> _tokenServiceMock;
    private readonly ClientCredentialsCommandHandler _sut;

    public ClientCredentialsCommandHandlerTests()
    {
        _clientAppRepositoryMock = new Mock<IClientAppRepository>();
        _passwordHasherMock = new Mock<IPasswordHasher>();
        _tokenServiceMock = new Mock<ITokenService>();
        _sut = new ClientCredentialsCommandHandler(
            _clientAppRepositoryMock.Object,
            _passwordHasherMock.Object,
            _tokenServiceMock.Object);
    }

    [Fact]
    public async Task Handle_ValidClient_ReturnsTokenResult()
    {
        // Arrange
        var command = new ClientCredentialsCommand("test-client", "test-secret");
        var fakeApp = TestDataHelper.CreateTestClientApp(command.ClientId);

        _clientAppRepositoryMock
            .Setup(r => r.FindByClientIdAsync(command.ClientId))
            .ReturnsAsync(fakeApp);

        _passwordHasherMock
            .Setup(h => h.Verify(command.ClientSecret, fakeApp.Secret.Value))
            .Returns(true);

        var expectedTokens = new TokenResult
        {
            AccessToken = "client_access_token",
            RefreshToken = "refresh_token",
            ExpiresAt = DateTime.UtcNow.AddMinutes(60),
            Role = "client",
            ClientId = command.ClientId
        };

        _tokenServiceMock
            .Setup(s => s.GenerateTokens(fakeApp.Id, fakeApp.Code, "client", fakeApp.Code))
            .Returns(expectedTokens);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.AccessToken.Should().Be(expectedTokens.AccessToken);

        _clientAppRepositoryMock.Verify(r => r.FindByClientIdAsync(command.ClientId), Times.Once);
        _passwordHasherMock.Verify(h => h.Verify(command.ClientSecret, fakeApp.Secret.Value), Times.Once);
    }

    [Fact]
    public async Task Handle_InvalidClient_ThrowsInvalidClientCredentialsException()
    {
        // Arrange
        var command = new ClientCredentialsCommand("invalid-client", "wrong-secret");

        _clientAppRepositoryMock
            .Setup(r => r.FindByClientIdAsync(command.ClientId))
            .ReturnsAsync((ClientApp?)null);

        // Act & Assert
        var act = async () => await _sut.Handle(command, CancellationToken.None);
        await act.Should().ThrowAsync<InvalidClientCredentialsException>();

        _clientAppRepositoryMock.Verify(r => r.FindByClientIdAsync(command.ClientId), Times.Once);
        _tokenServiceMock.Verify(s => s.GenerateTokens(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }
}

public class RefreshTokenCommandHandlerTests
{
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepositoryMock;
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IRoleRepository> _roleRepositoryMock;
    private readonly Mock<ITokenService> _tokenServiceMock;
    private readonly RefreshTokenCommandHandler _sut;

    public RefreshTokenCommandHandlerTests()
    {
        _refreshTokenRepositoryMock = new Mock<IRefreshTokenRepository>();
        _userRepositoryMock = new Mock<IUserRepository>();
        _roleRepositoryMock = new Mock<IRoleRepository>();
        _tokenServiceMock = new Mock<ITokenService>();
        _sut = new RefreshTokenCommandHandler(
            _refreshTokenRepositoryMock.Object,
            _userRepositoryMock.Object,
            _roleRepositoryMock.Object,
            _tokenServiceMock.Object);
    }

    [Fact]
    public async Task Handle_ValidRefreshToken_ReturnsNewTokens()
    {
        // Arrange
        var command = new RefreshTokenCommand("valid_refresh_token", "test-client");
        var fakeUser = TestDataHelper.CreateTestUser("user@example.com");
        var refreshToken = new RefreshToken(fakeUser.Id, command.RefreshToken, DateTime.UtcNow.AddDays(7), command.ClientId ?? "web");

        _refreshTokenRepositoryMock
            .Setup(r => r.FindAsync(command.RefreshToken))
            .ReturnsAsync(refreshToken);

        _userRepositoryMock
            .Setup(r => r.FindByIdAsync(refreshToken.UserId))
            .ReturnsAsync(fakeUser);

        // Setup role repository mock - handler now resolves role code
        var fakeRole = TestDataHelper.CreateTestRole("role_user", "User");
        _roleRepositoryMock
            .Setup(r => r.FindByIdAsync(fakeUser.RoleId!))
            .ReturnsAsync(fakeRole);

        var expectedTokens = new TokenResult
        {
            AccessToken = "new_access_token",
            RefreshToken = "new_refresh_token",
            ExpiresAt = DateTime.UtcNow.AddMinutes(30),
            Role = "role_user", // Now uses role code instead of RoleId
            ClientId = command.ClientId
        };

        _tokenServiceMock
            .Setup(s => s.GenerateTokens(fakeUser.Id, fakeUser.Email.Value, "role_user", command.ClientId))
            .Returns(expectedTokens);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.AccessToken.Should().Be(expectedTokens.AccessToken);
        result.RefreshToken.Should().Be(expectedTokens.RefreshToken);

        _refreshTokenRepositoryMock.Verify(r => r.FindAsync(command.RefreshToken), Times.Once);
        _refreshTokenRepositoryMock.Verify(r => r.AddAsync(It.IsAny<RefreshToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_InvalidRefreshToken_ThrowsInvalidRefreshTokenException()
    {
        // Arrange
        var command = new RefreshTokenCommand("invalid_refresh_token", "test-client");

        _refreshTokenRepositoryMock
            .Setup(r => r.FindAsync(command.RefreshToken))
            .ReturnsAsync((RefreshToken?)null);

        // Act & Assert
        var act = async () => await _sut.Handle(command, CancellationToken.None);
        await act.Should().ThrowAsync<InvalidRefreshTokenException>();

        _refreshTokenRepositoryMock.Verify(r => r.FindAsync(command.RefreshToken), Times.Once);
        _tokenServiceMock.Verify(s => s.GenerateTokens(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }
}