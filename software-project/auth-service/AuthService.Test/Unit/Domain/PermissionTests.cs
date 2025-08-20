using AuthService.Domain.Entities;
using FluentAssertions;

namespace AuthService.Test.Unit.Domain;

public class PermissionTests
{
    [Fact]
    public void Permission_Constructor_ShouldCreateValidPermission()
    {
        // Arrange
        var code = "perm_test_action";
        var name = "Test Action";
        var description = "Test description";

        // Act
        var permission = new Permission(code, name, description);

        // Assert
        permission.Code.Should().Be(code);
        permission.Name.Should().Be(name);
        permission.Description.Should().Be(description);
        permission.Id.Should().NotBeNullOrEmpty();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Permission_Constructor_ShouldThrowWhenCodeIsInvalid(string invalidCode)
    {
        // Act & Assert
        var act = () => new Permission(invalidCode, "Test Name", "Description");
        act.Should().Throw<ArgumentException>()
            .WithMessage("Permission code cannot be null or empty*");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Permission_Constructor_ShouldThrowWhenNameIsInvalid(string invalidName)
    {
        // Act & Assert
        var act = () => new Permission("perm_test", invalidName, "Description");
        act.Should().Throw<ArgumentException>()
            .WithMessage("Permission name cannot be null or empty*");
    }

    [Theory]
    [InlineData("invalid_code")]
    [InlineData("test_perm")]
    [InlineData("permission_")]
    public void Permission_Constructor_ShouldThrowWhenCodeDoesNotStartWithPerm(string invalidCode)
    {
        // Act & Assert
        var act = () => new Permission(invalidCode, "Test Name", "Description");
        act.Should().Throw<ArgumentException>()
            .WithMessage("Permission code must start with 'perm_'*");
    }

    [Fact]
    public void Permission_UpdateDetails_ShouldUpdateNameAndDescription()
    {
        // Arrange
        var permission = new Permission("perm_test", "Original Name", "Original Description");
        var newName = "Updated Name";
        var newDescription = "Updated Description";

        // Act
        permission.UpdateDetails(newName, newDescription);

        // Assert
        permission.Name.Should().Be(newName);
        permission.Description.Should().Be(newDescription);
        permission.Code.Should().Be("perm_test"); // Code should remain unchanged
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Permission_UpdateDetails_ShouldThrowWhenNameIsInvalid(string invalidName)
    {
        // Arrange
        var permission = new Permission("perm_test", "Original Name", "Original Description");

        // Act & Assert
        var act = () => permission.UpdateDetails(invalidName, "New Description");
        act.Should().Throw<ArgumentException>()
            .WithMessage("Permission name cannot be null or empty*");
    }
}