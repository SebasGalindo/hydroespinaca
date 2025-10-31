using AuthService.Application.Features.Users.Commands.CreateUser;
using AuthService.Domain.Entities;
using AuthService.Domain.Interfaces;
using AuthService.Domain.ValueObjects;
using AutoMapper;
using HydroEspinaca.Shared.DTOs.Authentication;

namespace AuthService.Application.Tests.Features.Users;

public class CreateUserCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IPasswordHasher> _passwordHasherMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly CreateUserCommandHandler _handler;

    public CreateUserCommandHandlerTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _passwordHasherMock = new Mock<IPasswordHasher>();
        _mapperMock = new Mock<IMapper>();
        _handler = new CreateUserCommandHandler(
            _userRepositoryMock.Object,
            _passwordHasherMock.Object,
            _mapperMock.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_ShouldCreateUserAndReturnDto()
    {
        // Arrange
        var command = new CreateUserCommand(
            "testuser",
            "test@example.com",
            "Password123!",
            "role123"
        );

        var hashedPassword = "$2a$11$hashedPassword";
        var userDto = new UserResponseDto
        {
            Id = "user123",
            Username = "testuser",
            Email = "test@example.com"
        };

        _userRepositoryMock.Setup(x => x.FindByEmailAsync(command.Email))
            .ReturnsAsync((User)null!);
        _passwordHasherMock.Setup(x => x.Hash(command.Password))
            .Returns(hashedPassword);
        _mapperMock.Setup(x => x.Map<UserResponseDto>(It.IsAny<User>()))
            .Returns(userDto);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().Be(userDto);
        _userRepositoryMock.Verify(x => x.FindByEmailAsync(command.Email), Times.Once);
        _passwordHasherMock.Verify(x => x.Hash(command.Password), Times.Once);
        _userRepositoryMock.Verify(x => x.CreateAsync(It.IsAny<User>()), Times.Once);
        _mapperMock.Verify(x => x.Map<UserResponseDto>(It.IsAny<User>()), Times.Once);
    }

    [Fact]
    public async Task Handle_EmailAlreadyExists_ShouldThrowArgumentException()
    {
        // Arrange
        var command = new CreateUserCommand(
            "testuser",
            "existing@example.com",
            "Password123!",
            null
        );

        var existingUser = new User("existinguser", new Email("existing@example.com"), new HashedPassword("$2a$11$hash"));

        _userRepositoryMock.Setup(x => x.FindByEmailAsync(command.Email))
            .ReturnsAsync(existingUser);

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage($"Ya existe un usuario con el email '{command.Email}'");
        _userRepositoryMock.Verify(x => x.CreateAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithoutRoleId_ShouldCreateUserWithNullRole()
    {
        // Arrange
        var command = new CreateUserCommand(
            "testuser",
            "test@example.com",
            "Password123!",
            null
        );

        var hashedPassword = "$2a$11$hashedPassword";
        var userDto = new UserResponseDto
        {
            Id = "user123",
            Username = "testuser",
            Email = "test@example.com",
            RoleId = null
        };

        _userRepositoryMock.Setup(x => x.FindByEmailAsync(command.Email))
            .ReturnsAsync((User)null!);
        _passwordHasherMock.Setup(x => x.Hash(command.Password))
            .Returns(hashedPassword);
        _mapperMock.Setup(x => x.Map<UserResponseDto>(It.IsAny<User>()))
            .Returns(userDto);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.RoleId.Should().BeNull();
        _userRepositoryMock.Verify(x => x.CreateAsync(It.Is<User>(u => u.RoleId == null)), Times.Once);
    }
}
