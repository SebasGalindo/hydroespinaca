using AuthService.Application.Features.Permissions.Queries.GetPermission;
using AuthService.Domain.Entities;
using AuthService.Domain.Interfaces;
using AutoMapper;
using HydroEspinaca.Shared.DTOs.Authentication;

namespace AuthService.Application.Tests.Features.Permissions;

public class GetPermissionQueryHandlerTests
{
    private readonly Mock<IPermissionRepository> _permissionRepositoryMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly GetPermissionQueryHandler _handler;

    public GetPermissionQueryHandlerTests()
    {
        _permissionRepositoryMock = new Mock<IPermissionRepository>();
        _mapperMock = new Mock<IMapper>();
        _handler = new GetPermissionQueryHandler(_permissionRepositoryMock.Object, _mapperMock.Object);
    }

    [Fact]
    public async Task Handle_PermissionFoundByCode_ShouldReturnMappedPermission()
    {
        // Arrange
        var permissionCode = "user:read";
        var permission = new Permission(permissionCode, "Read Users", "Can read user data");

        var permissionDto = new PermissionResponseDto
        {
            Id = "1",
            Code = permissionCode,
            Name = "Read Users"
        };

        _permissionRepositoryMock.Setup(x => x.FindByCodeAsync(permissionCode)).ReturnsAsync(permission);
        _mapperMock.Setup(x => x.Map<PermissionResponseDto>(permission)).Returns(permissionDto);

        var query = new GetPermissionQuery(permissionCode);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().Be(permissionDto);
        _permissionRepositoryMock.Verify(x => x.FindByCodeAsync(permissionCode), Times.Once);
        _mapperMock.Verify(x => x.Map<PermissionResponseDto>(permission), Times.Once);
    }

    [Fact]
    public async Task Handle_PermissionFoundById_ShouldReturnMappedPermission()
    {
        // Arrange
        var permissionId = "507f1f77bcf86cd799439011";
        var permission = new Permission("user:read", "Read Users", "Can read user data");

        var permissionDto = new PermissionResponseDto
        {
            Id = permissionId,
            Code = "user:read",
            Name = "Read Users"
        };

        _permissionRepositoryMock.Setup(x => x.FindByCodeAsync(permissionId)).ReturnsAsync((Permission)null!);
        _permissionRepositoryMock.Setup(x => x.FindByIdAsync(permissionId)).ReturnsAsync(permission);
        _mapperMock.Setup(x => x.Map<PermissionResponseDto>(permission)).Returns(permissionDto);

        var query = new GetPermissionQuery(permissionId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().Be(permissionDto);
        _permissionRepositoryMock.Verify(x => x.FindByCodeAsync(permissionId), Times.Once);
        _permissionRepositoryMock.Verify(x => x.FindByIdAsync(permissionId), Times.Once);
        _mapperMock.Verify(x => x.Map<PermissionResponseDto>(permission), Times.Once);
    }

    [Fact]
    public async Task Handle_PermissionNotFound_ShouldReturnNull()
    {
        // Arrange
        var permissionCode = "nonexistent:permission";
        _permissionRepositoryMock.Setup(x => x.FindByCodeAsync(permissionCode)).ReturnsAsync((Permission)null!);

        var query = new GetPermissionQuery(permissionCode);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeNull();
        _permissionRepositoryMock.Verify(x => x.FindByCodeAsync(permissionCode), Times.Once);
        _mapperMock.Verify(x => x.Map<PermissionResponseDto>(It.IsAny<Permission>()), Times.Never);
    }
}
