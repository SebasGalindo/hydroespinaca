using AuthService.Application.Features.Roles.Commands.UpdateRole;
using AuthService.Domain.Entities;
using AuthService.Domain.Interfaces;
using AutoMapper;
using HydroEspinaca.Shared.DTOs.Authentication;
using Microsoft.Extensions.Logging;

namespace AuthService.Application.Tests.Features.Roles.Commands;

public class UpdateRoleCommandHandlerTests
{
    private readonly Mock<IRoleRepository> _roleRepositoryMock;
    private readonly Mock<IPermissionRepository> _permissionRepositoryMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly Mock<ILogger<UpdateRoleCommandHandler>> _loggerMock;
    private readonly UpdateRoleCommandHandler _handler;

    public UpdateRoleCommandHandlerTests()
    {
        _roleRepositoryMock = new Mock<IRoleRepository>();
        _permissionRepositoryMock = new Mock<IPermissionRepository>();
        _mapperMock = new Mock<IMapper>();
        _loggerMock = new Mock<ILogger<UpdateRoleCommandHandler>>();
        _handler = new UpdateRoleCommandHandler(
            _roleRepositoryMock.Object,
            _permissionRepositoryMock.Object,
            _mapperMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_RoleFoundByCode_ShouldUpdateAndReturnDto()
    {
        // Arrange
        var roleCode = "role_ADMIN";
        var role = new Role(roleCode, "Administrator", new List<string> { "PERM_READ" });
        role.SetId("role123");

        var command = new UpdateRoleCommand(roleCode, "Super Admin", new List<string> { "PERM_READ", "PERM_WRITE" });

        var expectedDto = new RoleResponseDto
        {
            Id = "role123",
            Code = roleCode,
            Name = "Super Admin",
            PermissionCodes = new List<string> { "PERM_READ", "PERM_WRITE" }
        };

        _roleRepositoryMock.Setup(x => x.FindByCodeAsync(roleCode)).ReturnsAsync(role);
        _permissionRepositoryMock.Setup(x => x.GetNonExistingCodesAsync(command.PermissionCodes))
            .ReturnsAsync(new List<string>());
        _roleRepositoryMock.Setup(x => x.UpdateAsync(role)).Returns(Task.CompletedTask);
        _mapperMock.Setup(x => x.Map<RoleResponseDto>(role)).Returns(expectedDto);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(expectedDto);
        _roleRepositoryMock.Verify(x => x.FindByCodeAsync(roleCode), Times.Once);
        _permissionRepositoryMock.Verify(x => x.GetNonExistingCodesAsync(command.PermissionCodes), Times.Once);
        _roleRepositoryMock.Verify(x => x.UpdateAsync(role), Times.Once);
    }

    [Fact]
    public async Task Handle_RoleFoundById_ShouldUpdateAndReturnDto()
    {
        // Arrange
        var roleId = "507f1f77bcf86cd799439011";
        var role = new Role("role_ADMIN", "Administrator", new List<string> { "PERM_READ" });
        role.SetId(roleId);

        var command = new UpdateRoleCommand(roleId, "Super Admin", new List<string> { "PERM_READ", "PERM_WRITE" });

        var expectedDto = new RoleResponseDto
        {
            Id = roleId,
            Code = "role_ADMIN",
            Name = "Super Admin",
            PermissionCodes = new List<string> { "PERM_READ", "PERM_WRITE" }
        };

        _roleRepositoryMock.Setup(x => x.FindByCodeAsync(roleId)).ReturnsAsync((Role)null!);
        _roleRepositoryMock.Setup(x => x.FindByIdAsync(roleId)).ReturnsAsync(role);
        _permissionRepositoryMock.Setup(x => x.GetNonExistingCodesAsync(command.PermissionCodes))
            .ReturnsAsync(new List<string>());
        _roleRepositoryMock.Setup(x => x.UpdateAsync(role)).Returns(Task.CompletedTask);
        _mapperMock.Setup(x => x.Map<RoleResponseDto>(role)).Returns(expectedDto);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(expectedDto);
        _roleRepositoryMock.Verify(x => x.FindByCodeAsync(roleId), Times.Once);
        _roleRepositoryMock.Verify(x => x.FindByIdAsync(roleId), Times.Once);
    }

    [Fact]
    public async Task Handle_RoleNotFound_ShouldReturnNull()
    {
        // Arrange
        var roleCode = "NONEXISTENT";
        var command = new UpdateRoleCommand(roleCode, "Name", new List<string> { "PERM_READ" });

        _roleRepositoryMock.Setup(x => x.FindByCodeAsync(roleCode)).ReturnsAsync((Role)null!);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeNull();
        _roleRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Role>()), Times.Never);
    }

    [Fact]
    public async Task Handle_NonExistingPermissions_ShouldThrowArgumentException()
    {
        // Arrange
        var roleCode = "role_ADMIN";
        var role = new Role(roleCode, "Administrator", new List<string>());
        role.SetId("role123");

        var command = new UpdateRoleCommand(roleCode, "Admin", new List<string> { "PERM_READ", "PERM_NONEXISTENT" });

        _roleRepositoryMock.Setup(x => x.FindByCodeAsync(roleCode)).ReturnsAsync(role);
        _permissionRepositoryMock.Setup(x => x.GetNonExistingCodesAsync(command.PermissionCodes))
            .ReturnsAsync(new List<string> { "PERM_NONEXISTENT" });

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*PERM_NONEXISTENT*");
        _roleRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Role>()), Times.Never);
    }

    [Fact]
    public async Task Handle_DuplicatePermissionCodes_ShouldRemoveDuplicates()
    {
        // Arrange
        var roleCode = "role_ADMIN";
        var role = new Role(roleCode, "Administrator", new List<string>());
        role.SetId("role123");

        var command = new UpdateRoleCommand(roleCode, "Admin", new List<string> { "PERM_READ", "PERM_WRITE", "PERM_READ" }); // Duplicate PERM_READ

        var expectedDto = new RoleResponseDto
        {
            Id = "role123",
            Code = roleCode,
            Name = "Admin",
            PermissionCodes = new List<string> { "PERM_READ", "PERM_WRITE" }
        };

        _roleRepositoryMock.Setup(x => x.FindByCodeAsync(roleCode)).ReturnsAsync(role);
        _permissionRepositoryMock.Setup(x => x.GetNonExistingCodesAsync(It.IsAny<List<string>>()))
            .ReturnsAsync(new List<string>());
        _roleRepositoryMock.Setup(x => x.UpdateAsync(role)).Returns(Task.CompletedTask);
        _mapperMock.Setup(x => x.Map<RoleResponseDto>(role)).Returns(expectedDto);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        _roleRepositoryMock.Verify(x => x.UpdateAsync(role), Times.Once);
    }
}
