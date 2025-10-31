using AuthService.Application.Features.Permissions.Queries.GetAllPermissions;
using AuthService.Domain.Entities;
using AuthService.Domain.Interfaces;
using AutoMapper;
using HydroEspinaca.Shared.DTOs.Authentication;

namespace AuthService.Application.Tests.Features.Permissions;

public class GetAllPermissionsQueryHandlerTests
{
    private readonly Mock<IPermissionRepository> _permissionRepositoryMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly GetAllPermissionsQueryHandler _handler;

    public GetAllPermissionsQueryHandlerTests()
    {
        _permissionRepositoryMock = new Mock<IPermissionRepository>();
        _mapperMock = new Mock<IMapper>();
        _handler = new GetAllPermissionsQueryHandler(_permissionRepositoryMock.Object, _mapperMock.Object);
    }

    [Fact]
    public async Task Handle_WithPermissions_ShouldReturnMappedPermissions()
    {
        // Arrange
        var permissions = new List<Permission>
        {
            new Permission("READ_USERS", "Read Users", "Can read user data"),
            new Permission("WRITE_USERS", "Write Users", "Can write user data")
        };

        var permissionDtos = new List<PermissionResponseDto>
        {
            new PermissionResponseDto { Id = "1", Code = "READ_USERS", Name = "Read Users" },
            new PermissionResponseDto { Id = "2", Code = "WRITE_USERS", Name = "Write Users" }
        };

        _permissionRepositoryMock.Setup(x => x.GetAllAsync()).ReturnsAsync(permissions);
        _mapperMock.Setup(x => x.Map<List<PermissionResponseDto>>(permissions)).Returns(permissionDtos);

        var query = new GetAllPermissionsQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.Should().BeEquivalentTo(permissionDtos);
        _permissionRepositoryMock.Verify(x => x.GetAllAsync(), Times.Once);
        _mapperMock.Verify(x => x.Map<List<PermissionResponseDto>>(permissions), Times.Once);
    }

    [Fact]
    public async Task Handle_WithNoPermissions_ShouldReturnEmptyList()
    {
        // Arrange
        var permissions = new List<Permission>();
        var permissionDtos = new List<PermissionResponseDto>();

        _permissionRepositoryMock.Setup(x => x.GetAllAsync()).ReturnsAsync(permissions);
        _mapperMock.Setup(x => x.Map<List<PermissionResponseDto>>(permissions)).Returns(permissionDtos);

        var query = new GetAllPermissionsQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
        _permissionRepositoryMock.Verify(x => x.GetAllAsync(), Times.Once);
    }
}
