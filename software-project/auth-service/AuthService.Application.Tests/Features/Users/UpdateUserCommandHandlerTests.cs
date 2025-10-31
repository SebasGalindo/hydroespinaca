using AuthService.Application.Features.Users.Commands.UpdateUser;
using AuthService.Domain.Entities;
using AuthService.Domain.Interfaces;
using AuthService.Domain.ValueObjects;
using AutoMapper;
using HydroEspinaca.Shared.DTOs.Authentication;
using Microsoft.Extensions.Logging;

namespace AuthService.Application.Tests.Features.Users;

public class UpdateUserCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IPasswordHasher> _passwordHasherMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly Mock<ILogger<UpdateUserCommandHandler>> _loggerMock;
    private readonly UpdateUserCommandHandler _handler;

    public UpdateUserCommandHandlerTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _passwordHasherMock = new Mock<IPasswordHasher>();
        _mapperMock = new Mock<IMapper>();
        _loggerMock = new Mock<ILogger<UpdateUserCommandHandler>>();
        _handler = new UpdateUserCommandHandler(
            _userRepositoryMock.Object,
            _passwordHasherMock.Object,
            _mapperMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_UpdateUsername_ShouldUpdateUsernameAndReturnDto()
    {
        // Arrange
        var userId = "507f1f77bcf86cd799439011";
        var user = new User("oldusername", new Email("test@example.com"), new HashedPassword("$2a$11$hash"));
        user.SetId(userId);

        var command = new UpdateUserCommand(userId, "newusername");

        var userDto = new UserResponseDto
        {
            Id = userId,
            Username = "newusername",
            Email = "test@example.com"
        };

        _userRepositoryMock.Setup(x => x.FindByIdAsync(userId)).ReturnsAsync(user);
        _mapperMock.Setup(x => x.Map<UserResponseDto>(user)).Returns(userDto);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Username.Should().Be("newusername");
        _userRepositoryMock.Verify(x => x.UpdateAsync(user), Times.Once);
    }

    [Fact]
    public async Task Handle_UpdateEmail_ShouldUpdateEmailAndReturnDto()
    {
        // Arrange
        var userId = "507f1f77bcf86cd799439011";
        var user = new User("testuser", new Email("old@example.com"), new HashedPassword("$2a$11$hash"));
        user.SetId(userId);

        var command = new UpdateUserCommand(userId, null, "new@example.com");

        var userDto = new UserResponseDto
        {
            Id = userId,
            Username = "testuser",
            Email = "new@example.com"
        };

        _userRepositoryMock.Setup(x => x.FindByIdAsync(userId)).ReturnsAsync(user);
        _userRepositoryMock.Setup(x => x.FindByEmailAsync("new@example.com")).ReturnsAsync((User)null!);
        _mapperMock.Setup(x => x.Map<UserResponseDto>(user)).Returns(userDto);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        _userRepositoryMock.Verify(x => x.FindByEmailAsync("new@example.com"), Times.Once);
        _userRepositoryMock.Verify(x => x.UpdateAsync(user), Times.Once);
    }

    [Fact]
    public async Task Handle_UpdatePassword_ShouldHashAndUpdatePassword()
    {
        // Arrange
        var userId = "507f1f77bcf86cd799439011";
        var user = new User("testuser", new Email("test@example.com"), new HashedPassword("$2a$11$oldhash"));
        user.SetId(userId);

        var command = new UpdateUserCommand(userId, null, null, "NewPassword123!");
        var newHashedPassword = "$2a$11$newhash";

        var userDto = new UserResponseDto { Id = userId, Username = "testuser" };

        _userRepositoryMock.Setup(x => x.FindByIdAsync(userId)).ReturnsAsync(user);
        _passwordHasherMock.Setup(x => x.Hash("NewPassword123!")).Returns(newHashedPassword);
        _mapperMock.Setup(x => x.Map<UserResponseDto>(user)).Returns(userDto);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        _passwordHasherMock.Verify(x => x.Hash("NewPassword123!"), Times.Once);
        _userRepositoryMock.Verify(x => x.UpdateAsync(user), Times.Once);
    }

    [Fact]
    public async Task Handle_UserNotFound_ShouldThrowArgumentException()
    {
        // Arrange
        var userId = "507f1f77bcf86cd799439011";
        var command = new UpdateUserCommand(userId, "newusername");

        _userRepositoryMock.Setup(x => x.FindByIdAsync(userId)).ReturnsAsync((User)null!);

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage($"No se encontró el usuario con ID '{userId}'");
        _userRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task Handle_EmailAlreadyExists_ShouldThrowArgumentException()
    {
        // Arrange
        var userId = "507f1f77bcf86cd799439011";
        var user = new User("testuser", new Email("old@example.com"), new HashedPassword("$2a$11$hash"));
        user.SetId(userId);

        var existingUser = new User("otheruser", new Email("existing@example.com"), new HashedPassword("$2a$11$hash"));

        var command = new UpdateUserCommand(userId, null, "existing@example.com");

        _userRepositoryMock.Setup(x => x.FindByIdAsync(userId)).ReturnsAsync(user);
        _userRepositoryMock.Setup(x => x.FindByEmailAsync("existing@example.com")).ReturnsAsync(existingUser);

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Ya existe un usuario con el email 'existing@example.com'");
    }

    [Fact]
    public async Task Handle_UpdateRoleId_ShouldUpdateRoleId()
    {
        // Arrange
        var userId = "507f1f77bcf86cd799439011";
        var user = new User("testuser", new Email("test@example.com"), new HashedPassword("$2a$11$hash"), "oldRole");
        user.SetId(userId);

        var command = new UpdateUserCommand(userId, null, null, null, "newRole");

        var userDto = new UserResponseDto { Id = userId, RoleId = "newRole" };

        _userRepositoryMock.Setup(x => x.FindByIdAsync(userId)).ReturnsAsync(user);
        _mapperMock.Setup(x => x.Map<UserResponseDto>(user)).Returns(userDto);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        _userRepositoryMock.Verify(x => x.UpdateAsync(user), Times.Once);
    }
}
