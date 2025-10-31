using AuthService.Application.Features.Roles.Commands.CreateRole;
using AuthService.Domain.Entities;
using AuthService.Domain.Interfaces;
using AutoMapper;
using HydroEspinaca.Shared.DTOs.Authentication;
using Microsoft.Extensions.Logging;

namespace AuthService.Application.Tests.Features.Roles;

public class CreateRoleCommandHandlerTests
{
    private readonly Mock<IRoleRepository> _roleRepositoryMock;
    private readonly Mock<IPermissionRepository> _permissionRepositoryMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly Mock<ILogger<CreateRoleCommandHandler>> _loggerMock;
    private readonly CreateRoleCommandHandler _handler;

    public CreateRoleCommandHandlerTests()
    {
        _roleRepositoryMock = new Mock<IRoleRepository>();
        _permissionRepositoryMock = new Mock<IPermissionRepository>();
        _mapperMock = new Mock<IMapper>();
        _loggerMock = new Mock<ILogger<CreateRoleCommandHandler>>();
        _handler = new CreateRoleCommandHandler(
            _roleRepositoryMock.Object,
            _permissionRepositoryMock.Object,
            _mapperMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_ShouldCreateRoleAndReturnDto()
    {
        // Arrange
        var command = new CreateRoleCommand(
            "role_admin",
            "Administrator",
            new List<string> { "user:read", "user:write" }
        );

        var roleDto = new RoleResponseDto
        {
            Id = "role123",
            Code = "role_admin",
            Name = "Administrator"
        };

        _roleRepositoryMock.Setup(x => x.FindByCodeAsync(command.Code))
            .ReturnsAsync((Role)null!);
        _permissionRepositoryMock.Setup(x => x.GetNonExistingCodesAsync(command.PermissionCodes))
            .ReturnsAsync(new List<string>());
        _mapperMock.Setup(x => x.Map<RoleResponseDto>(It.IsAny<Role>()))
            .Returns(roleDto);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().Be(roleDto);
        _roleRepositoryMock.Verify(x => x.FindByCodeAsync(command.Code), Times.Once);
        _permissionRepositoryMock.Verify(x => x.GetNonExistingCodesAsync(command.PermissionCodes), Times.Once);
        _roleRepositoryMock.Verify(x => x.CreateAsync(It.IsAny<Role>()), Times.Once);
    }

    [Fact]
    public async Task Handle_RoleCodeAlreadyExists_ShouldThrowArgumentException()
    {
        // Arrange
        var command = new CreateRoleCommand(
            "role_admin",
            "Administrator",
            new List<string> { "user:read" }
        );

        var existingRole = new Role("role_admin", "Existing Admin", new List<string>());

        _roleRepositoryMock.Setup(x => x.FindByCodeAsync(command.Code))
            .ReturnsAsync(existingRole);

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage($"Ya existe un rol con el código '{command.Code}'");
        _roleRepositoryMock.Verify(x => x.CreateAsync(It.IsAny<Role>()), Times.Never);
    }

    [Fact]
    public async Task Handle_NonExistingPermissionCodes_ShouldThrowArgumentException()
    {
        // Arrange
        var command = new CreateRoleCommand(
            "role_admin",
            "Administrator",
            new List<string> { "user:read", "nonexistent:permission" }
        );

        var nonExistingCodes = new List<string> { "nonexistent:permission" };

        _roleRepositoryMock.Setup(x => x.FindByCodeAsync(command.Code))
            .ReturnsAsync((Role)null!);
        _permissionRepositoryMock.Setup(x => x.GetNonExistingCodesAsync(command.PermissionCodes))
            .ReturnsAsync(nonExistingCodes);

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("No se encontraron los permisos con códigos: nonexistent:permission");
        _roleRepositoryMock.Verify(x => x.CreateAsync(It.IsAny<Role>()), Times.Never);
    }

    [Fact]
    public async Task Handle_DuplicatePermissionCodes_ShouldCreateRoleWithDistinctCodes()
    {
        // Arrange
        var command = new CreateRoleCommand(
            "role_admin",
            "Administrator",
            new List<string> { "user:read", "user:read", "user:write" }
        );

        var roleDto = new RoleResponseDto
        {
            Id = "role123",
            Code = "role_admin",
            Name = "Administrator"
        };

        _roleRepositoryMock.Setup(x => x.FindByCodeAsync(command.Code))
            .ReturnsAsync((Role)null!);
        _permissionRepositoryMock.Setup(x => x.GetNonExistingCodesAsync(It.IsAny<List<string>>()))
            .ReturnsAsync(new List<string>());
        _mapperMock.Setup(x => x.Map<RoleResponseDto>(It.IsAny<Role>()))
            .Returns(roleDto);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        _roleRepositoryMock.Verify(x => x.CreateAsync(It.IsAny<Role>()), Times.Once);
    }
}
