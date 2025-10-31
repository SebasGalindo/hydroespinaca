using AuthService.Application.Features.Permissions.Commands.CreatePermission;
using AuthService.Domain.Entities;
using AuthService.Domain.Interfaces;
using AutoMapper;
using HydroEspinaca.Shared.DTOs.Authentication;

namespace AuthService.Application.Tests.Features.Permissions;

public class CreatePermissionCommandHandlerTests
{
    private readonly Mock<IPermissionRepository> _permissionRepositoryMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly CreatePermissionCommandHandler _handler;

    public CreatePermissionCommandHandlerTests()
    {
        _permissionRepositoryMock = new Mock<IPermissionRepository>();
        _mapperMock = new Mock<IMapper>();
        _handler = new CreatePermissionCommandHandler(
            _permissionRepositoryMock.Object,
            _mapperMock.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_ShouldCreatePermissionAndReturnDto()
    {
        // Arrange
        var command = new CreatePermissionCommand(
            "user:read",
            "Read Users",
            "Allows reading user information"
        );

        var permissionDto = new PermissionResponseDto
        {
            Id = "perm123",
            Code = "user:read",
            Name = "Read Users"
        };

        _permissionRepositoryMock.Setup(x => x.FindByCodeAsync(command.Code))
            .ReturnsAsync((Permission)null!);
        _mapperMock.Setup(x => x.Map<PermissionResponseDto>(It.IsAny<Permission>()))
            .Returns(permissionDto);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().Be(permissionDto);
        _permissionRepositoryMock.Verify(x => x.FindByCodeAsync(command.Code), Times.Once);
        _permissionRepositoryMock.Verify(x => x.CreateAsync(It.IsAny<Permission>()), Times.Once);
    }

    [Fact]
    public async Task Handle_PermissionCodeAlreadyExists_ShouldThrowArgumentException()
    {
        // Arrange
        var command = new CreatePermissionCommand(
            "user:read",
            "Read Users",
            "Allows reading user information"
        );

        var existingPermission = new Permission("user:read", "Existing Permission", null);

        _permissionRepositoryMock.Setup(x => x.FindByCodeAsync(command.Code))
            .ReturnsAsync(existingPermission);

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage($"Ya existe un permiso con el código '{command.Code}'");
        _permissionRepositoryMock.Verify(x => x.CreateAsync(It.IsAny<Permission>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithoutDescription_ShouldCreatePermissionWithNullDescription()
    {
        // Arrange
        var command = new CreatePermissionCommand(
            "user:read",
            "Read Users",
            null
        );

        var permissionDto = new PermissionResponseDto
        {
            Id = "perm123",
            Code = "user:read",
            Name = "Read Users",
            Description = null
        };

        _permissionRepositoryMock.Setup(x => x.FindByCodeAsync(command.Code))
            .ReturnsAsync((Permission)null!);
        _mapperMock.Setup(x => x.Map<PermissionResponseDto>(It.IsAny<Permission>()))
            .Returns(permissionDto);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        _permissionRepositoryMock.Verify(x => x.CreateAsync(It.Is<Permission>(p => p.Description == null)), Times.Once);
    }
}
