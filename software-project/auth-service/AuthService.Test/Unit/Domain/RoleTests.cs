using AuthService.Domain.Entities;
using FluentAssertions;

namespace AuthService.Test.Unit.Domain;

public class RoleTests
{
    [Fact]
    public void Role_Constructor_ShouldCreateValidRole()
    {
        // Arrange
        var code = "role_test";
        var name = "Test Role";
        var permissions = new List<string> { "507f1f77bcf86cd799439011", "507f1f77bcf86cd799439012" };

        // Act
        var role = new Role(code, name, permissions);

        // Assert
        role.Code.Should().Be(code);
        role.Name.Should().Be(name);
        role.Permissions.Should().BeEquivalentTo(permissions);
        role.Id.Should().NotBeNullOrEmpty();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Role_Constructor_ShouldThrowWhenCodeIsInvalid(string invalidCode)
    {
        // Act & Assert
        var act = () => new Role(invalidCode, "Test Name", new List<string>());
        act.Should().Throw<ArgumentException>()
            .WithMessage("Role code cannot be null or empty*");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Role_Constructor_ShouldThrowWhenNameIsInvalid(string invalidName)
    {
        // Act & Assert
        var act = () => new Role("role_test", invalidName, new List<string>());
        act.Should().Throw<ArgumentException>()
            .WithMessage("Role name cannot be null or empty*");
    }

    [Theory]
    [InlineData("invalid_code")]
    [InlineData("test_role")]
    [InlineData("roles_")]
    public void Role_Constructor_ShouldThrowWhenCodeDoesNotStartWithRole(string invalidCode)
    {
        // Act & Assert
        var act = () => new Role(invalidCode, "Test Name", new List<string>());
        act.Should().Throw<ArgumentException>()
            .WithMessage("Role code must start with 'role_'*");
    }

    [Fact]
    public void Role_Constructor_ShouldHandleNullPermissions()
    {
        // Act
        var role = new Role("role_test", "Test Role", null!);

        // Assert
        role.Permissions.Should().NotBeNull();
        role.Permissions.Should().BeEmpty();
    }

    [Fact]
    public void Role_UpdateName_ShouldUpdateName()
    {
        // Arrange
        var role = new Role("role_test", "Original Name", new List<string>());
        var newName = "Updated Name";

        // Act
        role.UpdateName(newName);

        // Assert
        role.Name.Should().Be(newName);
        role.Code.Should().Be("role_test"); // Code should remain unchanged
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Role_UpdateName_ShouldThrowWhenNameIsInvalid(string invalidName)
    {
        // Arrange
        var role = new Role("role_test", "Original Name", new List<string>());

        // Act & Assert
        var act = () => role.UpdateName(invalidName);
        act.Should().Throw<ArgumentException>()
            .WithMessage("Role name cannot be null or empty*");
    }

    [Fact]
    public void Role_SetPermissions_ShouldUpdatePermissions()
    {
        // Arrange
        var role = new Role("role_test", "Test Role", new List<string>());
        var newPermissions = new List<string> { "507f1f77bcf86cd799439011", "507f1f77bcf86cd799439012" };

        // Act
        role.SetPermissions(newPermissions);

        // Assert
        role.Permissions.Should().BeEquivalentTo(newPermissions);
    }

    [Fact]
    public void Role_SetPermissions_ShouldHandleNullPermissions()
    {
        // Arrange
        var role = new Role("role_test", "Test Role", new List<string> { "507f1f77bcf86cd799439011" });

        // Act
        role.SetPermissions(null!);

        // Assert
        role.Permissions.Should().NotBeNull();
        role.Permissions.Should().BeEmpty();
    }
}