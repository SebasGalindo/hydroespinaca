using AuthService.Application.Features.Users.Commands.DeleteUser;
using AuthService.Domain.Entities;
using AuthService.Domain.Interfaces;
using AuthService.Domain.ValueObjects;

namespace AuthService.Application.Tests.Features.Users;

public class DeleteUserCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly DeleteUserCommandHandler _handler;

    public DeleteUserCommandHandlerTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _handler = new DeleteUserCommandHandler(_userRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_UserExists_ShouldDeleteUser()
    {
        // Arrange
        var userId = "507f1f77bcf86cd799439011";
        var user = new User("testuser", new Email("test@example.com"), new HashedPassword("$2a$11$hash"));
        user.SetId(userId);

        var command = new DeleteUserCommand(userId);

        _userRepositoryMock.Setup(x => x.FindByIdAsync(userId)).ReturnsAsync(user);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _userRepositoryMock.Verify(x => x.FindByIdAsync(userId), Times.Once);
        _userRepositoryMock.Verify(x => x.DeleteAsync(userId), Times.Once);
    }

    [Fact]
    public async Task Handle_UserNotFound_ShouldThrowArgumentException()
    {
        // Arrange
        var userId = "507f1f77bcf86cd799439011";
        var command = new DeleteUserCommand(userId);

        _userRepositoryMock.Setup(x => x.FindByIdAsync(userId)).ReturnsAsync((User)null!);

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage($"No se encontró el usuario con ID '{userId}'");
        _userRepositoryMock.Verify(x => x.DeleteAsync(It.IsAny<string>()), Times.Never);
    }
}
