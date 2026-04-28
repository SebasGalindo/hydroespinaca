using AuthService.Application.Exceptions;
using AuthService.Application.Features.Authentication.Commands.Login;
using AuthService.Domain.Entities;
using AuthService.Domain.Enums;
using AuthService.Domain.Interfaces;
using AuthService.Domain.Settings;
using AuthService.Domain.ValueObjects;
using HydroEspinaca.Shared.DTOs.Authentication;
using Microsoft.Extensions.Options;

namespace AuthService.Application.Tests.Features.Authentication;

public class LoginCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IRoleRepository> _roleRepositoryMock;
    private readonly Mock<IPasswordHasher> _passwordHasherMock;
    private readonly Mock<ITokenService> _tokenServiceMock;
    private readonly Mock<IUserSessionService> _sessionServiceMock;
    private readonly Mock<IUserSessionRepository> _sessionRepositoryMock;
    private readonly Mock<IOptions<JwtSettings>> _jwtSettingsMock;
    private readonly LoginCommandHandler _handler;

    public LoginCommandHandlerTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _roleRepositoryMock = new Mock<IRoleRepository>();
        _passwordHasherMock = new Mock<IPasswordHasher>();
        _tokenServiceMock = new Mock<ITokenService>();
        _sessionServiceMock = new Mock<IUserSessionService>();
        _sessionRepositoryMock = new Mock<IUserSessionRepository>();
        _jwtSettingsMock = new Mock<IOptions<JwtSettings>>();

        var jwtSettings = new JwtSettings
        {
            RefreshTokenExpiryDays = 7
        };
        _jwtSettingsMock.Setup(x => x.Value).Returns(jwtSettings);

        _handler = new LoginCommandHandler(
            _userRepositoryMock.Object,
            _roleRepositoryMock.Object,
            _passwordHasherMock.Object,
            _tokenServiceMock.Object,
            _sessionServiceMock.Object,
            _sessionRepositoryMock.Object,
            _jwtSettingsMock.Object);
    }

    [Fact]
    public async Task Handle_ValidCredentials_ShouldReturnTokenResult()
    {
        // Arrange
        var email = "test@example.com";
        var password = "password123";
        var command = new LoginCommand(email, password);

        var user = new User("testuser", new Email(email), new HashedPassword("$2a$11$hash"), "role123");
        user.SetId("user123");
        user.AcceptTerms();

        var role = new Role("role_admin", "Admin", new List<string> { "read:all", "write:all" });

        _userRepositoryMock.Setup(x => x.FindByEmailAsync(email)).ReturnsAsync(user);
        _passwordHasherMock.Setup(x => x.Verify(It.IsAny<string>(), password)).Returns(true);
        _roleRepositoryMock.Setup(x => x.FindByIdAsync("role123")).ReturnsAsync(role);

        _tokenServiceMock.Setup(x => x.GenerateTokensAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string?>(),
            TokenType.User,
            It.IsAny<bool>()))
            .ReturnsAsync(new TokenResult
            {
                AccessToken = "access-token",
                RefreshToken = "refresh-token",
                ExpiresAt = DateTime.UtcNow.AddHours(1),
                Role = "admin",
                ClientId = null,
                Scopes = new[] { "read:all", "write:all" }
            });

        _sessionServiceMock.Setup(x => x.GetActiveUserSessionsAsync(It.IsAny<string>()))
            .ReturnsAsync(new List<UserSession>());

        _sessionServiceMock.Setup(x => x.CreateSessionAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<DateTime>(),
            It.IsAny<string?>(),
            It.IsAny<string?>(),
            It.IsAny<string?>()))
            .ReturnsAsync(new UserSession(
                "user123",
                "client-id",
                "session-id",
                "refresh-token",
                "access-token",
                DateTime.UtcNow.AddHours(1),
                null,
                null,
                null));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.AccessToken.Should().Be("access-token");
        result.RefreshToken.Should().Be("refresh-token");
        result.Username.Should().Be("testuser");
        result.Email.Should().Be(email);

        _userRepositoryMock.Verify(x => x.FindByEmailAsync(email), Times.Once);
        _passwordHasherMock.Verify(x => x.Verify(It.IsAny<string>(), password), Times.Once);
        _sessionServiceMock.Verify(x => x.CreateSessionAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<DateTime>(),
            It.IsAny<string?>(),
            It.IsAny<string?>(),
            It.IsAny<string?>()), Times.Once);
    }

    [Fact]
    public async Task Handle_UserNotFound_ShouldThrowInvalidCredentialsException()
    {
        // Arrange
        var command = new LoginCommand("nonexistent@example.com", "password");

        _userRepositoryMock.Setup(x => x.FindByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync((User)null!);

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidCredentialsException>();
        _passwordHasherMock.Verify(x => x.Verify(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Handle_InvalidPassword_ShouldThrowInvalidCredentialsException()
    {
        // Arrange
        var command = new LoginCommand("test@example.com", "wrong-password");

        var user = new User("testuser", new Email("test@example.com"), new HashedPassword("$2a$11$hash"));

        _userRepositoryMock.Setup(x => x.FindByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync(user);

        _passwordHasherMock.Setup(x => x.Verify(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(false);

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidCredentialsException>();
        _tokenServiceMock.Verify(x => x.GenerateTokensAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string?>(),
            It.IsAny<TokenType>(),
            It.IsAny<string[]?>(),
            It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    public async Task Handle_UserWithoutRole_ShouldUseDefaultUserRole()
    {
        // Arrange
        var email = "test@example.com";
        var command = new LoginCommand(email, "password");

        var user = new User("testuser", new Email(email), new HashedPassword("$2a$11$hash"));
        user.SetId("user123");
        user.AcceptTerms();

        _userRepositoryMock.Setup(x => x.FindByEmailAsync(email)).ReturnsAsync(user);
        _passwordHasherMock.Setup(x => x.Verify(It.IsAny<string>(), It.IsAny<string>())).Returns(true);

        _tokenServiceMock.Setup(x => x.GenerateTokensAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string?>(),
            TokenType.User,
            It.IsAny<bool>()))
            .ReturnsAsync(new TokenResult
            {
                AccessToken = "access-token",
                RefreshToken = "refresh-token",
                ExpiresAt = DateTime.UtcNow.AddHours(1),
                Role = "user",
                ClientId = null,
                Scopes = Array.Empty<string>()
            });

        _sessionServiceMock.Setup(x => x.GetActiveUserSessionsAsync(It.IsAny<string>()))
            .ReturnsAsync(new List<UserSession>());

        _sessionServiceMock.Setup(x => x.CreateSessionAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<DateTime>(),
            It.IsAny<string?>(),
            It.IsAny<string?>(),
            It.IsAny<string?>()))
            .ReturnsAsync(new UserSession(
                "user123",
                "client-id",
                "session-id",
                "refresh-token",
                "access-token",
                DateTime.UtcNow.AddHours(1),
                null,
                null,
                null));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        _tokenServiceMock.Verify(x => x.GenerateTokensAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            "user",
            It.IsAny<string?>(),
            TokenType.User,
            It.IsAny<bool>()), Times.Once);
    }

    [Fact]
    public async Task Handle_MaxSessionsReached_ShouldRevokeOldestSession()
    {
        // Arrange
        var email = "test@example.com";
        var command = new LoginCommand(email, "password");

        var user = new User("testuser", new Email(email), new HashedPassword("$2a$11$hash"));
        user.SetId("user123");
        user.AcceptTerms();

        var oldSession1 = new UserSession(
            "user123",
            "client1",
            "session1",
            "refresh-token-1",
            "access-token-1",
            DateTime.UtcNow.AddDays(7),
            "ip1",
            "agent1",
            null);

        var oldSession2 = new UserSession(
            "user123",
            "client2",
            "session2",
            "refresh-token-2",
            "access-token-2",
            DateTime.UtcNow.AddDays(7),
            "ip2",
            "agent2",
            null);

        var activeSessions = new List<UserSession> { oldSession1, oldSession2 };

        _userRepositoryMock.Setup(x => x.FindByEmailAsync(email)).ReturnsAsync(user);
        _passwordHasherMock.Setup(x => x.Verify(It.IsAny<string>(), It.IsAny<string>())).Returns(true);
        _sessionServiceMock.Setup(x => x.GetActiveUserSessionsAsync("user123"))
            .ReturnsAsync(activeSessions);

        _tokenServiceMock.Setup(x => x.GenerateTokensAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string?>(),
            TokenType.User,
            It.IsAny<bool>()))
            .ReturnsAsync(new TokenResult
            {
                AccessToken = "access-token",
                RefreshToken = "refresh-token",
                ExpiresAt = DateTime.UtcNow.AddHours(1),
                Role = "user",
                ClientId = null,
                Scopes = Array.Empty<string>()
            });

        _sessionServiceMock.Setup(x => x.CreateSessionAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<DateTime>(),
            It.IsAny<string?>(),
            It.IsAny<string?>(),
            It.IsAny<string?>()))
            .ReturnsAsync(new UserSession(
                "user123",
                "client-id",
                "session-id",
                "refresh-token",
                "access-token",
                DateTime.UtcNow.AddHours(1),
                null,
                null,
                null));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        _sessionRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<UserSession>()), Times.Once);
    }
}
