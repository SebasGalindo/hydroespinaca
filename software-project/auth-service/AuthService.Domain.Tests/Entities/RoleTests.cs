using AuthService.Domain.Entities;

namespace AuthService.Domain.Tests.Entities;

public class RoleTests
{
    [Fact]
    public void Constructor_ValidParameters_ShouldCreateRoleInstance()
    {
        // Arrange
        var code = "role_admin";
        var name = "Administrator";
        var permissions = new List<string> { "read:all", "write:all", "delete:all" };

        // Act
        var role = new Role(code, name, permissions);

        // Assert
        role.Should().NotBeNull();
        role.Name.Should().Be(name);
        role.Code.Should().Be(code);
        role.Permissions.Should().BeEquivalentTo(permissions);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Constructor_NullOrEmptyName_ShouldThrowArgumentException(string name)
    {
        // Arrange
        var code = "role_admin";
        var permissions = new List<string> { "read:all" };

        // Act
        Action act = () => new Role(code, name, permissions);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("Role name cannot be null or empty*");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Constructor_NullOrEmptyCode_ShouldThrowArgumentException(string code)
    {
        // Arrange
        var name = "Administrator";
        var permissions = new List<string> { "read:all" };

        // Act
        Action act = () => new Role(code, name, permissions);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("Role code cannot be null or empty*");
    }

    [Fact]
    public void Constructor_CodeWithoutPrefix_ShouldThrowArgumentException()
    {
        // Arrange
        var code = "admin"; // without role_ prefix
        var name = "Administrator";

        // Act
        Action act = () => new Role(code, name, null);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("Role code must start with 'role_'*");
    }

    [Fact]
    public void Constructor_NullPermissions_ShouldCreateRoleWithEmptyPermissions()
    {
        // Arrange
        var code = "role_guest";
        var name = "Guest";

        // Act
        var role = new Role(code, name, null!);

        // Assert
        role.Permissions.Should().NotBeNull();
        role.Permissions.Should().BeEmpty();
    }

    [Fact]
    public void SetId_ValidId_ShouldSetRoleId()
    {
        // Arrange
        var role = new Role("role_admin", "Admin", new List<string> { "read:all" });
        var roleId = "role123";

        // Act
        role.SetId(roleId);

        // Assert
        role.Id.Should().Be(roleId);
    }

    [Fact]
    public void UpdateName_ValidName_ShouldUpdateRoleName()
    {
        // Arrange
        var role = new Role("role_test", "OldName", new List<string> { "read:all" });
        var newName = "NewName";

        // Act
        role.UpdateName(newName);

        // Assert
        role.Name.Should().Be(newName);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void UpdateName_NullOrEmptyName_ShouldThrowArgumentException(string name)
    {
        // Arrange
        var role = new Role("role_admin", "Admin", new List<string> { "read:all" });

        // Act
        Action act = () => role.UpdateName(name);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("Role name cannot be null or empty*");
    }

    [Fact]
    public void SetPermissions_ValidPermissions_ShouldUpdatePermissions()
    {
        // Arrange
        var role = new Role("role_admin", "Admin", new List<string> { "read:all" });
        var newPermissions = new List<string> { "read:all", "write:all", "delete:all" };

        // Act
        role.SetPermissions(newPermissions);

        // Assert
        role.Permissions.Should().BeEquivalentTo(newPermissions);
    }

    [Fact]
    public void SetPermissions_NullPermissions_ShouldSetEmptyPermissions()
    {
        // Arrange
        var role = new Role("role_admin", "Admin", new List<string> { "read:all", "write:all" });

        // Act
        role.SetPermissions(null!);

        // Assert
        role.Permissions.Should().NotBeNull();
        role.Permissions.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_EmptyPermissions_ShouldCreateRoleWithEmptyPermissions()
    {
        // Arrange
        var permissions = new List<string>();

        // Act
        var role = new Role("role_guest", "Guest", permissions);

        // Assert
        role.Permissions.Should().BeEmpty();
    }
}
