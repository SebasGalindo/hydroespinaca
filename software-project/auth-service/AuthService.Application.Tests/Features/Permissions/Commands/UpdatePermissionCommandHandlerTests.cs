using AuthService.Application.Features.Permissions.Commands.UpdatePermission;
using AuthService.Domain.Entities;
using AuthService.Domain.Interfaces;
using AutoMapper;
using HydroEspinaca.Shared.DTOs.Authentication;

namespace AuthService.Application.Tests.Features.Permissions.Commands;

public class UpdatePermissionCommandHandlerTests
{
    private readonly Mock<IPermissionRepository> _permissionRepositoryMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly UpdatePermissionCommandHandler _handler;

    public UpdatePermissionCommandHandlerTests()
    {
        _permissionRepositoryMock = new Mock<IPermissionRepository>();
        _mapperMock = new Mock<IMapper>();
        _handler = new UpdatePermissionCommandHandler(
            _permissionRepositoryMock.Object,
            _mapperMock.Object);
    }

    [Fact]
    public async Task Handle_PermissionFoundByCode_ShouldUpdateAndReturnDto()
    {
        // Arrange
        var permissionCode = "PERM_CREATE";
        var permission = new Permission(permissionCode, "Old Name", "Old Description");
        permission.SetId("perm123");

        var command = new UpdatePermissionCommand(permissionCode, "New Name", "New Description");

        var expectedDto = new PermissionResponseDto
        {
            Id = "perm123",
            Code = permissionCode,
            Name = "New Name",
            Description = "New Description"
        };

        _permissionRepositoryMock.Setup(x => x.FindByCodeAsync(permissionCode)).ReturnsAsync(permission);
        _permissionRepositoryMock.Setup(x => x.UpdateAsync(permission)).Returns(Task.CompletedTask);
        _mapperMock.Setup(x => x.Map<PermissionResponseDto>(permission)).Returns(expectedDto);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(expectedDto);
        _permissionRepositoryMock.Verify(x => x.FindByCodeAsync(permissionCode), Times.Once);
        _permissionRepositoryMock.Verify(x => x.UpdateAsync(permission), Times.Once);
        _mapperMock.Verify(x => x.Map<PermissionResponseDto>(permission), Times.Once);
    }

    [Fact]
    public async Task Handle_PermissionFoundById_ShouldUpdateAndReturnDto()
    {
        // Arrange
        var permissionId = "507f1f77bcf86cd799439011";
        var permission = new Permission("PERM_CREATE", "Old Name", "Old Description");
        permission.SetId(permissionId);

        var command = new UpdatePermissionCommand(permissionId, "New Name", "New Description");

        var expectedDto = new PermissionResponseDto
        {
            Id = permissionId,
            Code = "PERM_CREATE",
            Name = "New Name",
            Description = "New Description"
        };

        _permissionRepositoryMock.Setup(x => x.FindByCodeAsync(permissionId)).ReturnsAsync((Permission)null!);
        _permissionRepositoryMock.Setup(x => x.FindByIdAsync(permissionId)).ReturnsAsync(permission);
        _permissionRepositoryMock.Setup(x => x.UpdateAsync(permission)).Returns(Task.CompletedTask);
        _mapperMock.Setup(x => x.Map<PermissionResponseDto>(permission)).Returns(expectedDto);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(expectedDto);
        _permissionRepositoryMock.Verify(x => x.FindByCodeAsync(permissionId), Times.Once);
        _permissionRepositoryMock.Verify(x => x.FindByIdAsync(permissionId), Times.Once);
        _permissionRepositoryMock.Verify(x => x.UpdateAsync(permission), Times.Once);
    }

    [Fact]
    public async Task Handle_PermissionNotFound_ShouldReturnNull()
    {
        // Arrange
        var permissionCode = "PERM_NONEXISTENT";
        var command = new UpdatePermissionCommand(permissionCode, "New Name", "New Description");

        _permissionRepositoryMock.Setup(x => x.FindByCodeAsync(permissionCode)).ReturnsAsync((Permission)null!);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeNull();
        _permissionRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Permission>()), Times.Never);
        _mapperMock.Verify(x => x.Map<PermissionResponseDto>(It.IsAny<Permission>()), Times.Never);
    }

    [Fact]
    public async Task Handle_InvalidObjectId_ShouldReturnNull()
    {
        // Arrange
        var invalidId = "not-an-object-id";
        var command = new UpdatePermissionCommand(invalidId, "New Name", "New Description");

        _permissionRepositoryMock.Setup(x => x.FindByCodeAsync(invalidId)).ReturnsAsync((Permission)null!);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeNull();
        _permissionRepositoryMock.Verify(x => x.FindByIdAsync(It.IsAny<string>()), Times.Never);
        _permissionRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Permission>()), Times.Never);
    }
}
