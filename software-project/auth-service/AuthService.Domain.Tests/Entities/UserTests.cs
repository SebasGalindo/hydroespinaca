using AuthService.Domain.Entities;
using AuthService.Domain.ValueObjects;

namespace AuthService.Domain.Tests.Entities;

public class UserTests
{
    private Email CreateValidEmail() => new Email("test@example.com");
    private HashedPassword CreateValidPassword() => new HashedPassword("$2a$11$hashedPassword");

    [Fact]
    public void Constructor_ValidParameters_ShouldCreateUserInstance()
    {
        // Arrange
        var username = "testuser";
        var email = CreateValidEmail();
        var password = CreateValidPassword();
        var roleId = "role123";

        // Act
        var user = new User(username, email, password, roleId);

        // Assert
        user.Should().NotBeNull();
        user.Username.Should().Be(username);
        user.Email.Should().Be(email);
        user.Password.Should().Be(password);
        user.RoleId.Should().Be(roleId);
    }

    [Fact]
    public void Constructor_WithoutRoleId_ShouldCreateUserWithNullRole()
    {
        // Arrange
        var username = "testuser";
        var email = CreateValidEmail();
        var password = CreateValidPassword();

        // Act
        var user = new User(username, email, password);

        // Assert
        user.RoleId.Should().BeNull();
    }

    [Fact]
    public void Constructor_NullUsername_ShouldThrowArgumentNullException()
    {
        // Arrange
        var email = CreateValidEmail();
        var password = CreateValidPassword();

        // Act
        Action act = () => new User(null!, email, password);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("username");
    }

    [Fact]
    public void Constructor_NullEmail_ShouldThrowArgumentNullException()
    {
        // Arrange
        var username = "testuser";
        var password = CreateValidPassword();

        // Act
        Action act = () => new User(username, null!, password);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("email");
    }

    [Fact]
    public void Constructor_NullPassword_ShouldThrowArgumentNullException()
    {
        // Arrange
        var username = "testuser";
        var email = CreateValidEmail();

        // Act
        Action act = () => new User(username, email, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("password");
    }

    [Fact]
    public void SetId_ValidId_ShouldSetUserId()
    {
        // Arrange
        var user = new User("testuser", CreateValidEmail(), CreateValidPassword());
        var userId = "user123";

        // Act
        user.SetId(userId);

        // Assert
        user.Id.Should().Be(userId);
    }

    [Fact]
    public void UpdateUsername_ValidUsername_ShouldUpdateUsername()
    {
        // Arrange
        var user = new User("oldusername", CreateValidEmail(), CreateValidPassword());
        var newUsername = "newusername";

        // Act
        user.UpdateUsername(newUsername);

        // Assert
        user.Username.Should().Be(newUsername);
    }

    [Fact]
    public void UpdateUsername_NullUsername_ShouldThrowArgumentNullException()
    {
        // Arrange
        var user = new User("testuser", CreateValidEmail(), CreateValidPassword());

        // Act
        Action act = () => user.UpdateUsername(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("username");
    }

    [Fact]
    public void UpdateEmail_ValidEmail_ShouldUpdateEmail()
    {
        // Arrange
        var user = new User("testuser", CreateValidEmail(), CreateValidPassword());
        var newEmail = new Email("newemail@example.com");

        // Act
        user.UpdateEmail(newEmail);

        // Assert
        user.Email.Should().Be(newEmail);
    }

    [Fact]
    public void UpdateEmail_NullEmail_ShouldThrowArgumentNullException()
    {
        // Arrange
        var user = new User("testuser", CreateValidEmail(), CreateValidPassword());

        // Act
        Action act = () => user.UpdateEmail(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("email");
    }

    [Fact]
    public void UpdatePassword_ValidPassword_ShouldUpdatePassword()
    {
        // Arrange
        var user = new User("testuser", CreateValidEmail(), CreateValidPassword());
        var newPassword = new HashedPassword("$2a$11$newHashedPassword");

        // Act
        user.UpdatePassword(newPassword);

        // Assert
        user.Password.Should().Be(newPassword);
    }

    [Fact]
    public void UpdatePassword_NullPassword_ShouldThrowArgumentNullException()
    {
        // Arrange
        var user = new User("testuser", CreateValidEmail(), CreateValidPassword());

        // Act
        Action act = () => user.UpdatePassword(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("password");
    }

    [Fact]
    public void UpdateRoleId_ValidRoleId_ShouldUpdateRoleId()
    {
        // Arrange
        var user = new User("testuser", CreateValidEmail(), CreateValidPassword(), "oldRole");
        var newRoleId = "newRole";

        // Act
        user.UpdateRoleId(newRoleId);

        // Assert
        user.RoleId.Should().Be(newRoleId);
    }

    [Fact]
    public void UpdateRoleId_NullRoleId_ShouldSetRoleIdToNull()
    {
        // Arrange
        var user = new User("testuser", CreateValidEmail(), CreateValidPassword(), "someRole");

        // Act
        user.UpdateRoleId(null);

        // Assert
        user.RoleId.Should().BeNull();
    }
}
