using AuthService.Application.Features.Roles.Commands.CreateRole;
using AuthService.Application.Features.Roles.Commands.UpdateRole;
using AuthService.Application.Features.Roles.Commands.DeleteRole;
using AuthService.Application.Features.Roles.Queries.GetRole;
using AuthService.Application.Features.Roles.Queries.GetAllRoles;
using AuthService.Application.Features.Roles.DTOs;
using AuthService.Domain.Entities;
using AuthService.Domain.Interfaces;
using AuthService.Test.Helpers;
using AutoMapper;
using FluentAssertions;
using HydroEspinaca.Shared.Errors;
using Moq;

namespace AuthService.Test.Unit.Application;

public class CreateRoleCommandHandlerTests
{
    private readonly Mock<IRoleRepository> _roleRepositoryMock;
    private readonly Mock<IPermissionRepository> _permissionRepositoryMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly CreateRoleCommandHandler _sut;

    public CreateRoleCommandHandlerTests()
    {
        _roleRepositoryMock = new Mock<IRoleRepository>();
        _permissionRepositoryMock = new Mock<IPermissionRepository>();
        _mapperMock = new Mock<IMapper>();
        _sut = new CreateRoleCommandHandler(_roleRepositoryMock.Object, _permissionRepositoryMock.Object, _mapperMock.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_ReturnsRoleResponseDto()
    {
        // Arrange
        var command = new CreateRoleCommand("role_test_123", "Test Role", new List<string>());
        var role = TestDataHelper.CreateTestRole(command.Code, command.Name);
        var expectedResponse = new RoleResponseDto
        {
            Id = role.Id,
            Code = role.Code,
            Name = role.Name,
            PermissionCodes = command.PermissionCodes
        };

        _roleRepositoryMock.Setup(r => r.FindByCodeAsync(command.Code))
            .ReturnsAsync((Role?)null);

        _roleRepositoryMock.Setup(r => r.CreateAsync(It.IsAny<Role>()))
            .Returns(Task.CompletedTask);

        _mapperMock.Setup(m => m.Map<RoleResponseDto>(It.IsAny<Role>()))
            .Returns(expectedResponse);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Code.Should().Be(command.Code);
        result.Name.Should().Be(command.Name);

        _roleRepositoryMock.Verify(r => r.CreateAsync(It.IsAny<Role>()), Times.Once);
    }
}

public class GetRoleQueryHandlerTests
{
    private readonly Mock<IRoleRepository> _roleRepositoryMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly GetRoleQueryHandler _sut;

    public GetRoleQueryHandlerTests()
    {
        _roleRepositoryMock = new Mock<IRoleRepository>();
        _mapperMock = new Mock<IMapper>();
        _sut = new GetRoleQueryHandler(_roleRepositoryMock.Object, _mapperMock.Object);
    }

    [Fact]
    public async Task Handle_ExistingCode_ReturnsRoleResponseDto()
    {
        // Arrange
        var query = new GetRoleQuery("role_test_admin");
        var role = TestDataHelper.CreateTestRole(query.IdOrCode);
        var expectedResponse = new RoleResponseDto
        {
            Id = role.Id,
            Code = role.Code,
            Name = role.Name,
            PermissionCodes = new List<string>()
        };

        _roleRepositoryMock.Setup(r => r.FindByCodeAsync(query.IdOrCode))
            .ReturnsAsync(role);

        _mapperMock.Setup(m => m.Map<RoleResponseDto>(role))
            .Returns(expectedResponse);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Code.Should().Be(query.IdOrCode);
        result.Name.Should().Be(role.Name);
    }

    [Fact]
    public async Task Handle_NonExistingCode_ReturnsNull()
    {
        // Arrange
        var query = new GetRoleQuery("role_non_existing");

        _roleRepositoryMock.Setup(r => r.FindByCodeAsync(query.IdOrCode))
            .ReturnsAsync((Role?)null);
        
        _roleRepositoryMock.Setup(r => r.FindByIdAsync(query.IdOrCode))
            .ReturnsAsync((Role?)null);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }
}

public class UpdateRoleCommandHandlerTests
{
    private readonly Mock<IRoleRepository> _roleRepositoryMock;
    private readonly Mock<IPermissionRepository> _permissionRepositoryMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly UpdateRoleCommandHandler _sut;

    public UpdateRoleCommandHandlerTests()
    {
        _roleRepositoryMock = new Mock<IRoleRepository>();
        _permissionRepositoryMock = new Mock<IPermissionRepository>();
        _mapperMock = new Mock<IMapper>();
        _sut = new UpdateRoleCommandHandler(_roleRepositoryMock.Object, _permissionRepositoryMock.Object, _mapperMock.Object);
    }

    [Fact]
    public async Task Handle_ExistingRole_UpdatesAndReturnsRole()
    {
        // Arrange
        var command = new UpdateRoleCommand("role_test_admin", "Updated Admin Role", new List<string>());
        var role = TestDataHelper.CreateTestRole(command.IdOrCode, "Original Admin Role");
        var expectedResponse = new RoleResponseDto
        {
            Id = role.Id,
            Code = role.Code,
            Name = command.Name,
            PermissionCodes = command.PermissionCodes
        };

        _roleRepositoryMock.Setup(r => r.FindByCodeAsync(command.IdOrCode))
            .ReturnsAsync(role);

        _permissionRepositoryMock.Setup(p => p.FindByCodesAsync(command.PermissionCodes))
            .ReturnsAsync(new List<Permission>());

        _mapperMock.Setup(m => m.Map<RoleResponseDto>(role))
            .Returns(expectedResponse);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be(command.Name);

        _roleRepositoryMock.Verify(r => r.UpdateAsync(role), Times.Once);
    }

    [Fact]
    public async Task Handle_NonExistingRole_ReturnsNull()
    {
        // Arrange
        var command = new UpdateRoleCommand("role_non_existing", "Updated Name", new List<string>());

        _roleRepositoryMock.Setup(r => r.FindByCodeAsync(command.IdOrCode))
            .ReturnsAsync((Role?)null);
        
        _roleRepositoryMock.Setup(r => r.FindByIdAsync(command.IdOrCode))
            .ReturnsAsync((Role?)null);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeNull();
        _roleRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Role>()), Times.Never);
    }
}

public class DeleteRoleCommandHandlerTests
{
    private readonly Mock<IRoleRepository> _repositoryMock;
    private readonly DeleteRoleCommandHandler _sut;

    public DeleteRoleCommandHandlerTests()
    {
        _repositoryMock = new Mock<IRoleRepository>();
        _sut = new DeleteRoleCommandHandler(_repositoryMock.Object);
    }

    [Fact]
    public async Task Handle_ExistingRole_DeletesAndReturnsTrue()
    {
        // Arrange
        var command = new DeleteRoleCommand("role_test_admin");
        var role = TestDataHelper.CreateTestRole(command.IdOrCode);

        _repositoryMock.Setup(r => r.FindByCodeAsync(command.IdOrCode))
            .ReturnsAsync(role);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        _repositoryMock.Verify(r => r.DeleteAsync(role.Id), Times.Once);
    }

    [Fact]
    public async Task Handle_NonExistingRole_ReturnsFalse()
    {
        // Arrange
        var command = new DeleteRoleCommand("role_non_existing");

        _repositoryMock.Setup(r => r.FindByCodeAsync(command.IdOrCode))
            .ReturnsAsync((Role?)null);
        
        _repositoryMock.Setup(r => r.FindByIdAsync(command.IdOrCode))
            .ReturnsAsync((Role?)null);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeFalse();
        _repositoryMock.Verify(r => r.DeleteAsync(It.IsAny<string>()), Times.Never);
    }
}

public class GetAllRolesQueryHandlerTests
{
    private readonly Mock<IRoleRepository> _roleRepositoryMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly GetAllRolesQueryHandler _sut;

    public GetAllRolesQueryHandlerTests()
    {
        _roleRepositoryMock = new Mock<IRoleRepository>();
        _mapperMock = new Mock<IMapper>();
        _sut = new GetAllRolesQueryHandler(_roleRepositoryMock.Object, _mapperMock.Object);
    }

    [Fact]
    public async Task Handle_ReturnsAllRoles()
    {
        // Arrange
        var query = new GetAllRolesQuery();
        var roles = new List<Role>
        {
            TestDataHelper.CreateTestRole("role_admin", "Administrator"),
            TestDataHelper.CreateTestRole("role_user", "User")
        };

        var expectedResponse = roles.Select(r => new RoleResponseDto
        {
            Id = r.Id,
            Code = r.Code,
            Name = r.Name,
            PermissionCodes = new List<string>()
        }).ToList();

        _roleRepositoryMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(roles);

        _mapperMock.Setup(m => m.Map<List<RoleResponseDto>>(roles))
            .Returns(expectedResponse);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.Should().Contain(r => r.Code == "role_admin");
        result.Should().Contain(r => r.Code == "role_user");
    }
}