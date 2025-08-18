using AuthService.Application.Features.Permissions.Commands.CreatePermission;
using AuthService.Application.Features.Permissions.Commands.UpdatePermission;
using AuthService.Application.Features.Permissions.Commands.DeletePermission;
using AuthService.Application.Features.Permissions.Queries.GetPermission;
using AuthService.Application.Features.Permissions.Queries.GetAllPermissions;
using AuthService.Application.Features.Permissions.DTOs;
using AuthService.Domain.Entities;
using AuthService.Domain.Interfaces;
using AuthService.Test.Helpers;
using AutoMapper;
using FluentAssertions;
using HydroEspinaca.Shared.Errors;
using Moq;

namespace AuthService.Test.Unit.Application;

public class CreatePermissionCommandHandlerTests
{
    private readonly Mock<IPermissionRepository> _repositoryMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly CreatePermissionCommandHandler _sut;

    public CreatePermissionCommandHandlerTests()
    {
        _repositoryMock = new Mock<IPermissionRepository>();
        _mapperMock = new Mock<IMapper>();
        _sut = new CreatePermissionCommandHandler(_repositoryMock.Object, _mapperMock.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_ReturnsPermissionResponseDto()
    {
        // Arrange
        var command = new CreatePermissionCommand("perm_test_123", "Test Permission", "Test description");
        var expectedResponse = new PermissionResponseDto
        {
            Id = "test-id",
            Code = command.Code,
            Name = command.Name,
            Description = command.Description
        };

        _repositoryMock.Setup(r => r.FindByCodeAsync(command.Code))
            .ReturnsAsync((Permission?)null);

        _repositoryMock.Setup(r => r.CreateAsync(It.IsAny<Permission>()))
            .Returns(Task.CompletedTask);

        _mapperMock.Setup(m => m.Map<PermissionResponseDto>(It.IsAny<Permission>()))
            .Returns(expectedResponse);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Code.Should().Be(command.Code);
        result.Name.Should().Be(command.Name);
        result.Description.Should().Be(command.Description);

        _repositoryMock.Verify(r => r.CreateAsync(It.IsAny<Permission>()), Times.Once);
    }
}

public class GetPermissionQueryHandlerTests
{
    private readonly Mock<IPermissionRepository> _repositoryMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly GetPermissionQueryHandler _sut;

    public GetPermissionQueryHandlerTests()
    {
        _repositoryMock = new Mock<IPermissionRepository>();
        _mapperMock = new Mock<IMapper>();
        _sut = new GetPermissionQueryHandler(_repositoryMock.Object, _mapperMock.Object);
    }

    [Fact]
    public async Task Handle_ExistingCode_ReturnsPermissionResponseDto()
    {
        // Arrange
        var query = new GetPermissionQuery("perm_test_action");
        var permission = TestDataHelper.CreateTestPermission(query.IdOrCode);
        var expectedResponse = new PermissionResponseDto
        {
            Id = permission.Id,
            Code = permission.Code,
            Name = permission.Name,
            Description = permission.Description
        };

        _repositoryMock.Setup(r => r.FindByCodeAsync(query.IdOrCode))
            .ReturnsAsync(permission);

        _mapperMock.Setup(m => m.Map<PermissionResponseDto>(permission))
            .Returns(expectedResponse);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Code.Should().Be(query.IdOrCode);
        result.Name.Should().Be(permission.Name);
    }

    [Fact]
    public async Task Handle_NonExistingCode_ReturnsNull()
    {
        // Arrange
        var query = new GetPermissionQuery("perm_non_existing");

        _repositoryMock.Setup(r => r.FindByCodeAsync(query.IdOrCode))
            .ReturnsAsync((Permission?)null);
        
        _repositoryMock.Setup(r => r.FindByIdAsync(query.IdOrCode))
            .ReturnsAsync((Permission?)null);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }
}

public class UpdatePermissionCommandHandlerTests
{
    private readonly Mock<IPermissionRepository> _repositoryMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly UpdatePermissionCommandHandler _sut;

    public UpdatePermissionCommandHandlerTests()
    {
        _repositoryMock = new Mock<IPermissionRepository>();
        _mapperMock = new Mock<IMapper>();
        _sut = new UpdatePermissionCommandHandler(_repositoryMock.Object, _mapperMock.Object);
    }

    [Fact]
    public async Task Handle_ExistingPermission_UpdatesAndReturnsPermission()
    {
        // Arrange
        var command = new UpdatePermissionCommand("perm_test_action", "Updated Name", "Updated description");
        var permission = TestDataHelper.CreateTestPermission(command.IdOrCode, "Original Name");
        var expectedResponse = new PermissionResponseDto
        {
            Id = permission.Id,
            Code = permission.Code,
            Name = command.Name,
            Description = command.Description
        };

        _repositoryMock.Setup(r => r.FindByCodeAsync(command.IdOrCode))
            .ReturnsAsync(permission);

        _mapperMock.Setup(m => m.Map<PermissionResponseDto>(permission))
            .Returns(expectedResponse);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be(command.Name);
        result.Description.Should().Be(command.Description);

        _repositoryMock.Verify(r => r.UpdateAsync(permission), Times.Once);
    }

    [Fact]
    public async Task Handle_NonExistingPermission_ReturnsNull()
    {
        // Arrange
        var command = new UpdatePermissionCommand("perm_non_existing", "Updated Name", "Updated description");

        _repositoryMock.Setup(r => r.FindByCodeAsync(command.IdOrCode))
            .ReturnsAsync((Permission?)null);
        
        _repositoryMock.Setup(r => r.FindByIdAsync(command.IdOrCode))
            .ReturnsAsync((Permission?)null);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeNull();
        _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Permission>()), Times.Never);
    }
}

public class DeletePermissionCommandHandlerTests
{
    private readonly Mock<IPermissionRepository> _repositoryMock;
    private readonly DeletePermissionCommandHandler _sut;

    public DeletePermissionCommandHandlerTests()
    {
        _repositoryMock = new Mock<IPermissionRepository>();
        _sut = new DeletePermissionCommandHandler(_repositoryMock.Object);
    }

    [Fact]
    public async Task Handle_ExistingPermission_DeletesAndReturnsTrue()
    {
        // Arrange
        var command = new DeletePermissionCommand("perm_test_action");
        var permission = TestDataHelper.CreateTestPermission(command.IdOrCode);

        _repositoryMock.Setup(r => r.FindByCodeAsync(command.IdOrCode))
            .ReturnsAsync(permission);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        _repositoryMock.Verify(r => r.DeleteAsync(permission.Id), Times.Once);
    }

    [Fact]
    public async Task Handle_NonExistingPermission_ReturnsFalse()
    {
        // Arrange
        var command = new DeletePermissionCommand("perm_non_existing");

        _repositoryMock.Setup(r => r.FindByCodeAsync(command.IdOrCode))
            .ReturnsAsync((Permission?)null);
        
        _repositoryMock.Setup(r => r.FindByIdAsync(command.IdOrCode))
            .ReturnsAsync((Permission?)null);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeFalse();
        _repositoryMock.Verify(r => r.DeleteAsync(It.IsAny<string>()), Times.Never);
    }
}

public class GetAllPermissionsQueryHandlerTests
{
    private readonly Mock<IPermissionRepository> _repositoryMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly GetAllPermissionsQueryHandler _sut;

    public GetAllPermissionsQueryHandlerTests()
    {
        _repositoryMock = new Mock<IPermissionRepository>();
        _mapperMock = new Mock<IMapper>();
        _sut = new GetAllPermissionsQueryHandler(_repositoryMock.Object, _mapperMock.Object);
    }

    [Fact]
    public async Task Handle_ReturnsAllPermissions()
    {
        // Arrange
        var query = new GetAllPermissionsQuery();
        var permissions = new List<Permission>
        {
            TestDataHelper.CreateTestPermission("perm_test_1", "Test Permission 1"),
            TestDataHelper.CreateTestPermission("perm_test_2", "Test Permission 2")
        };

        var expectedResponse = permissions.Select(p => new PermissionResponseDto
        {
            Id = p.Id,
            Code = p.Code,
            Name = p.Name,
            Description = p.Description
        }).ToList();

        _repositoryMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(permissions);

        _mapperMock.Setup(m => m.Map<List<PermissionResponseDto>>(permissions))
            .Returns(expectedResponse);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.Should().Contain(p => p.Code == "perm_test_1");
        result.Should().Contain(p => p.Code == "perm_test_2");
    }
}