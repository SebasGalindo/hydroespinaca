using AuthService.Application.Features.Roles.Queries.GetRole;
using AuthService.Domain.Entities;
using AuthService.Domain.Interfaces;
using AutoMapper;
using HydroEspinaca.Shared.DTOs.Authentication;

namespace AuthService.Application.Tests.Features.Roles;

public class GetRoleQueryHandlerTests
{
    private readonly Mock<IRoleRepository> _roleRepositoryMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly GetRoleQueryHandler _handler;

    public GetRoleQueryHandlerTests()
    {
        _roleRepositoryMock = new Mock<IRoleRepository>();
        _mapperMock = new Mock<IMapper>();
        _handler = new GetRoleQueryHandler(_roleRepositoryMock.Object, _mapperMock.Object);
    }

    [Fact]
    public async Task Handle_RoleFoundByCode_ShouldReturnMappedRole()
    {
        // Arrange
        var roleCode = "role_admin";
        var role = new Role(roleCode, "Administrator", new List<string> { "user:read", "user:write" });

        var roleDto = new RoleResponseDto
        {
            Id = "1",
            Code = roleCode,
            Name = "Administrator"
        };

        _roleRepositoryMock.Setup(x => x.FindByCodeAsync(roleCode)).ReturnsAsync(role);
        _mapperMock.Setup(x => x.Map<RoleResponseDto>(role)).Returns(roleDto);

        var query = new GetRoleQuery(roleCode);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().Be(roleDto);
        _roleRepositoryMock.Verify(x => x.FindByCodeAsync(roleCode), Times.Once);
        _mapperMock.Verify(x => x.Map<RoleResponseDto>(role), Times.Once);
    }

    [Fact]
    public async Task Handle_RoleFoundById_ShouldReturnMappedRole()
    {
        // Arrange
        var roleId = "507f1f77bcf86cd799439011";
        var role = new Role("role_admin", "Administrator", new List<string> { "user:read" });

        var roleDto = new RoleResponseDto
        {
            Id = roleId,
            Code = "role_admin",
            Name = "Administrator"
        };

        _roleRepositoryMock.Setup(x => x.FindByCodeAsync(roleId)).ReturnsAsync((Role)null!);
        _roleRepositoryMock.Setup(x => x.FindByIdAsync(roleId)).ReturnsAsync(role);
        _mapperMock.Setup(x => x.Map<RoleResponseDto>(role)).Returns(roleDto);

        var query = new GetRoleQuery(roleId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().Be(roleDto);
        _roleRepositoryMock.Verify(x => x.FindByCodeAsync(roleId), Times.Once);
        _roleRepositoryMock.Verify(x => x.FindByIdAsync(roleId), Times.Once);
        _mapperMock.Verify(x => x.Map<RoleResponseDto>(role), Times.Once);
    }

    [Fact]
    public async Task Handle_RoleNotFound_ShouldReturnNull()
    {
        // Arrange
        var roleCode = "role_nonexistent";
        _roleRepositoryMock.Setup(x => x.FindByCodeAsync(roleCode)).ReturnsAsync((Role)null!);

        var query = new GetRoleQuery(roleCode);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeNull();
        _roleRepositoryMock.Verify(x => x.FindByCodeAsync(roleCode), Times.Once);
        _mapperMock.Verify(x => x.Map<RoleResponseDto>(It.IsAny<Role>()), Times.Never);
    }
}
