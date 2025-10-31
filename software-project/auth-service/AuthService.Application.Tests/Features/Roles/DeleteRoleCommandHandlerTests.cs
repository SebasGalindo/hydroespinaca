using AuthService.Application.Features.Roles.Commands.DeleteRole;
using AuthService.Domain.Entities;
using AuthService.Domain.Exceptions;
using AuthService.Domain.Interfaces;

namespace AuthService.Application.Tests.Features.Roles;

public class DeleteRoleCommandHandlerTests
{
    private readonly Mock<IRoleRepository> _roleRepositoryMock;
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly DeleteRoleCommandHandler _handler;

    public DeleteRoleCommandHandlerTests()
    {
        _roleRepositoryMock = new Mock<IRoleRepository>();
        _userRepositoryMock = new Mock<IUserRepository>();
        _handler = new DeleteRoleCommandHandler(
            _roleRepositoryMock.Object,
            _userRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_RoleFoundByCodeWithNoUsers_ShouldDeleteRoleAndReturnTrue()
    {
        // Arrange
        var roleCode = "role_admin";
        var role = new Role(roleCode, "Administrator", new List<string>());
        role.SetId("role123");

        var command = new DeleteRoleCommand(roleCode);

        _roleRepositoryMock.Setup(x => x.FindByCodeAsync(roleCode)).ReturnsAsync(role);
        _userRepositoryMock.Setup(x => x.CountByRoleIdAsync(role.Id)).ReturnsAsync(0);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        _roleRepositoryMock.Verify(x => x.FindByCodeAsync(roleCode), Times.Once);
        _userRepositoryMock.Verify(x => x.CountByRoleIdAsync(role.Id), Times.Once);
        _roleRepositoryMock.Verify(x => x.DeleteAsync(role.Id), Times.Once);
    }

    [Fact]
    public async Task Handle_RoleFoundByIdWithNoUsers_ShouldDeleteRoleAndReturnTrue()
    {
        // Arrange
        var roleId = "507f1f77bcf86cd799439011";
        var role = new Role("role_admin", "Administrator", new List<string>());
        role.SetId(roleId);

        var command = new DeleteRoleCommand(roleId);

        _roleRepositoryMock.Setup(x => x.FindByCodeAsync(roleId)).ReturnsAsync((Role)null!);
        _roleRepositoryMock.Setup(x => x.FindByIdAsync(roleId)).ReturnsAsync(role);
        _userRepositoryMock.Setup(x => x.CountByRoleIdAsync(roleId)).ReturnsAsync(0);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        _roleRepositoryMock.Verify(x => x.FindByCodeAsync(roleId), Times.Once);
        _roleRepositoryMock.Verify(x => x.FindByIdAsync(roleId), Times.Once);
        _roleRepositoryMock.Verify(x => x.DeleteAsync(roleId), Times.Once);
    }

    [Fact]
    public async Task Handle_RoleNotFound_ShouldReturnFalse()
    {
        // Arrange
        var roleCode = "role_nonexistent";
        var command = new DeleteRoleCommand(roleCode);

        _roleRepositoryMock.Setup(x => x.FindByCodeAsync(roleCode)).ReturnsAsync((Role)null!);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeFalse();
        _roleRepositoryMock.Verify(x => x.DeleteAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Handle_RoleHasAssignedUsers_ShouldThrowRoleHasAssignedUsersException()
    {
        // Arrange
        var roleCode = "role_admin";
        var role = new Role(roleCode, "Administrator", new List<string>());
        role.SetId("role123");

        var command = new DeleteRoleCommand(roleCode);

        _roleRepositoryMock.Setup(x => x.FindByCodeAsync(roleCode)).ReturnsAsync(role);
        _userRepositoryMock.Setup(x => x.CountByRoleIdAsync(role.Id)).ReturnsAsync(5);

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<RoleHasAssignedUsersException>();
        _roleRepositoryMock.Verify(x => x.DeleteAsync(It.IsAny<string>()), Times.Never);
    }
}
