using AuthService.Application.Features.Roles.Queries.GetAllRoles;
using AuthService.Domain.Entities;
using AuthService.Domain.Interfaces;
using AutoMapper;
using HydroEspinaca.Shared.DTOs.Authentication;

namespace AuthService.Application.Tests.Features.Roles;

public class GetAllRolesQueryHandlerTests
{
    private readonly Mock<IRoleRepository> _roleRepositoryMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly GetAllRolesQueryHandler _handler;

    public GetAllRolesQueryHandlerTests()
    {
        _roleRepositoryMock = new Mock<IRoleRepository>();
        _mapperMock = new Mock<IMapper>();
        _handler = new GetAllRolesQueryHandler(_roleRepositoryMock.Object, _mapperMock.Object);
    }

    [Fact]
    public async Task Handle_WithRoles_ShouldReturnMappedRoles()
    {
        // Arrange
        var roles = new List<Role>
        {
            new Role("role_admin", "Admin", new List<string> { "read:all", "write:all" }),
            new Role("role_user", "User", new List<string> { "read:own" })
        };

        var roleDtos = new List<RoleResponseDto>
        {
            new RoleResponseDto { Id = "1", Code = "role_admin", Name = "Admin" },
            new RoleResponseDto { Id = "2", Code = "role_user", Name = "User" }
        };

        _roleRepositoryMock.Setup(x => x.GetAllAsync()).ReturnsAsync(roles);
        _mapperMock.Setup(x => x.Map<List<RoleResponseDto>>(roles)).Returns(roleDtos);

        var query = new GetAllRolesQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.Should().BeEquivalentTo(roleDtos);
        _roleRepositoryMock.Verify(x => x.GetAllAsync(), Times.Once);
        _mapperMock.Verify(x => x.Map<List<RoleResponseDto>>(roles), Times.Once);
    }

    [Fact]
    public async Task Handle_WithNoRoles_ShouldReturnEmptyList()
    {
        // Arrange
        var roles = new List<Role>();
        var roleDtos = new List<RoleResponseDto>();

        _roleRepositoryMock.Setup(x => x.GetAllAsync()).ReturnsAsync(roles);
        _mapperMock.Setup(x => x.Map<List<RoleResponseDto>>(roles)).Returns(roleDtos);

        var query = new GetAllRolesQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
        _roleRepositoryMock.Verify(x => x.GetAllAsync(), Times.Once);
    }
}
