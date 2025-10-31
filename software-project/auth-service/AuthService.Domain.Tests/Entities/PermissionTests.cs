using AuthService.Domain.Entities;

namespace AuthService.Domain.Tests.Entities;

public class PermissionTests
{
    [Fact]
    public void Constructor_ValidParameters_ShouldCreatePermissionInstance()
    {
        // Arrange
        var code = "READ_USERS";
        var name = "Read Users";
        var description = "Allows reading user information";

        // Act
        var permission = new Permission(code, name, description);

        // Assert
        permission.Should().NotBeNull();
        permission.Code.Should().Be(code);
        permission.Name.Should().Be(name);
        permission.Description.Should().Be(description);
        permission.Id.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Constructor_WithoutDescription_ShouldCreatePermissionWithNullDescription()
    {
        // Arrange
        var code = "READ_USERS";
        var name = "Read Users";

        // Act
        var permission = new Permission(code, name);

        // Assert
        permission.Description.Should().BeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Constructor_NullOrEmptyCode_ShouldThrowArgumentException(string code)
    {
        // Arrange
        var name = "Read Users";

        // Act
        Action act = () => new Permission(code, name);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("code");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Constructor_NullOrEmptyName_ShouldThrowArgumentException(string name)
    {
        // Arrange
        var code = "READ_USERS";

        // Act
        Action act = () => new Permission(code, name);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("name");
    }

    [Fact]
    public void SetId_ValidId_ShouldSetPermissionId()
    {
        // Arrange
        var permission = new Permission("READ_USERS", "Read Users");
        var permissionId = "permission123";

        // Act
        permission.SetId(permissionId);

        // Assert
        permission.Id.Should().Be(permissionId);
    }

    [Fact]
    public void UpdateDetails_ValidParameters_ShouldUpdateNameAndDescription()
    {
        // Arrange
        var permission = new Permission("READ_USERS", "Read Users", "Old description");
        var newName = "View Users";
        var newDescription = "New description";

        // Act
        permission.UpdateDetails(newName, newDescription);

        // Assert
        permission.Name.Should().Be(newName);
        permission.Description.Should().Be(newDescription);
    }

    [Fact]
    public void UpdateDetails_WithoutDescription_ShouldUpdateNameAndSetDescriptionToNull()
    {
        // Arrange
        var permission = new Permission("READ_USERS", "Read Users", "Some description");
        var newName = "View Users";

        // Act
        permission.UpdateDetails(newName);

        // Assert
        permission.Name.Should().Be(newName);
        permission.Description.Should().BeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void UpdateDetails_NullOrEmptyName_ShouldThrowArgumentException(string name)
    {
        // Arrange
        var permission = new Permission("READ_USERS", "Read Users");

        // Act
        Action act = () => permission.UpdateDetails(name);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("name");
    }
}
