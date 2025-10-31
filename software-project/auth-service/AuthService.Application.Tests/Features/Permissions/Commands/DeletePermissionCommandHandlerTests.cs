using AuthService.Application.Features.Permissions.Commands.DeletePermission;
using AuthService.Domain.Entities;
using AuthService.Domain.Interfaces;

namespace AuthService.Application.Tests.Features.Permissions.Commands;

public class DeletePermissionCommandHandlerTests
{
    private readonly Mock<IPermissionRepository> _permissionRepositoryMock;
    private readonly DeletePermissionCommandHandler _handler;

    public DeletePermissionCommandHandlerTests()
    {
        _permissionRepositoryMock = new Mock<IPermissionRepository>();
        _handler = new DeletePermissionCommandHandler(_permissionRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_PermissionFoundByCode_ShouldDeletePermissionAndReturnTrue()
    {
        // Arrange
        var permissionCode = "PERM_CREATE";
        var permission = new Permission(permissionCode, "Create Permission", "Allows creating resources");
        permission.SetId("perm123");

        var command = new DeletePermissionCommand(permissionCode);

        _permissionRepositoryMock.Setup(x => x.FindByCodeAsync(permissionCode)).ReturnsAsync(permission);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        _permissionRepositoryMock.Verify(x => x.FindByCodeAsync(permissionCode), Times.Once);
        _permissionRepositoryMock.Verify(x => x.DeleteAsync(permission.Id), Times.Once);
    }

    [Fact]
    public async Task Handle_PermissionFoundById_ShouldDeletePermissionAndReturnTrue()
    {
        // Arrange
        var permissionId = "507f1f77bcf86cd799439011";
        var permission = new Permission("PERM_CREATE", "Create Permission", "Allows creating resources");
        permission.SetId(permissionId);

        var command = new DeletePermissionCommand(permissionId);

        _permissionRepositoryMock.Setup(x => x.FindByCodeAsync(permissionId)).ReturnsAsync((Permission)null!);
        _permissionRepositoryMock.Setup(x => x.FindByIdAsync(permissionId)).ReturnsAsync(permission);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        _permissionRepositoryMock.Verify(x => x.FindByCodeAsync(permissionId), Times.Once);
        _permissionRepositoryMock.Verify(x => x.FindByIdAsync(permissionId), Times.Once);
        _permissionRepositoryMock.Verify(x => x.DeleteAsync(permissionId), Times.Once);
    }

    [Fact]
    public async Task Handle_PermissionNotFound_ShouldReturnFalse()
    {
        // Arrange
        var permissionCode = "PERM_NONEXISTENT";
        var command = new DeletePermissionCommand(permissionCode);

        _permissionRepositoryMock.Setup(x => x.FindByCodeAsync(permissionCode)).ReturnsAsync((Permission)null!);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeFalse();
        _permissionRepositoryMock.Verify(x => x.DeleteAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Handle_InvalidObjectId_ShouldReturnFalse()
    {
        // Arrange
        var invalidId = "not-an-object-id";
        var command = new DeletePermissionCommand(invalidId);

        _permissionRepositoryMock.Setup(x => x.FindByCodeAsync(invalidId)).ReturnsAsync((Permission)null!);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeFalse();
        _permissionRepositoryMock.Verify(x => x.FindByIdAsync(It.IsAny<string>()), Times.Never);
        _permissionRepositoryMock.Verify(x => x.DeleteAsync(It.IsAny<string>()), Times.Never);
    }
}
