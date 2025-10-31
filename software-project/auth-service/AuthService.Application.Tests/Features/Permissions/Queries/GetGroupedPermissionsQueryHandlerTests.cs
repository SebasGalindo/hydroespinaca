using AuthService.Application.Features.Permissions.Queries.GetGroupedPermissions;
using AuthService.Domain.Entities;
using AuthService.Domain.Interfaces;

namespace AuthService.Application.Tests.Features.Permissions.Queries;

public class GetGroupedPermissionsQueryHandlerTests
{
    private readonly Mock<IPermissionRepository> _permissionRepositoryMock;
    private readonly GetGroupedPermissionsQueryHandler _handler;

    public GetGroupedPermissionsQueryHandlerTests()
    {
        _permissionRepositoryMock = new Mock<IPermissionRepository>();
        _handler = new GetGroupedPermissionsQueryHandler(_permissionRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_WithGroupedPermissions_ShouldReturnGroupedPermissionsDto()
    {
        // Arrange
        var query = new GetGroupedPermissionsQuery();

        var permission1 = new Permission("USER_CREATE", "Create User", "Allows creating users");
        permission1.SetId("perm1");
        var permission2 = new Permission("USER_READ", "Read User", "Allows reading users");
        permission2.SetId("perm2");
        var permission3 = new Permission("PRODUCT_CREATE", "Create Product", "Allows creating products");
        permission3.SetId("perm3");

        var groupedPermissions = new List<GroupedPermissions>
        {
            new GroupedPermissions
            {
                Category = "User",
                Permissions = new List<Permission> { permission1, permission2 }
            },
            new GroupedPermissions
            {
                Category = "Product",
                Permissions = new List<Permission> { permission3 }
            }
        };

        _permissionRepositoryMock.Setup(x => x.GetGroupedAsync()).ReturnsAsync(groupedPermissions);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);

        var userGroup = result.FirstOrDefault(g => g.Category == "User");
        userGroup.Should().NotBeNull();
        userGroup!.Permissions.Should().HaveCount(2);
        userGroup.Permissions.Should().ContainSingle(p => p.Code == "USER_CREATE");
        userGroup.Permissions.Should().ContainSingle(p => p.Code == "USER_READ");

        var productGroup = result.FirstOrDefault(g => g.Category == "Product");
        productGroup.Should().NotBeNull();
        productGroup!.Permissions.Should().HaveCount(1);
        productGroup.Permissions.Should().ContainSingle(p => p.Code == "PRODUCT_CREATE");

        _permissionRepositoryMock.Verify(x => x.GetGroupedAsync(), Times.Once);
    }

    [Fact]
    public async Task Handle_WithEmptyGroups_ShouldReturnEmptyList()
    {
        // Arrange
        var query = new GetGroupedPermissionsQuery();
        var emptyGroupedPermissions = new List<GroupedPermissions>();

        _permissionRepositoryMock.Setup(x => x.GetGroupedAsync()).ReturnsAsync(emptyGroupedPermissions);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
        _permissionRepositoryMock.Verify(x => x.GetGroupedAsync(), Times.Once);
    }

    [Fact]
    public async Task Handle_PermissionsAreSortedByCode()
    {
        // Arrange
        var query = new GetGroupedPermissionsQuery();

        var permission1 = new Permission("USER_DELETE", "Delete User", "Allows deleting users");
        permission1.SetId("perm1");
        var permission2 = new Permission("USER_CREATE", "Create User", "Allows creating users");
        permission2.SetId("perm2");
        var permission3 = new Permission("USER_READ", "Read User", "Allows reading users");
        permission3.SetId("perm3");

        var groupedPermissions = new List<GroupedPermissions>
        {
            new GroupedPermissions
            {
                Category = "User",
                Permissions = new List<Permission> { permission1, permission2, permission3 }
            }
        };

        _permissionRepositoryMock.Setup(x => x.GetGroupedAsync()).ReturnsAsync(groupedPermissions);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        var userGroup = result.First();
        userGroup.Permissions.Should().HaveCount(3);

        // Verify they are sorted alphabetically by code
        userGroup.Permissions.ElementAt(0).Code.Should().Be("USER_CREATE");
        userGroup.Permissions.ElementAt(1).Code.Should().Be("USER_DELETE");
        userGroup.Permissions.ElementAt(2).Code.Should().Be("USER_READ");
    }
}
