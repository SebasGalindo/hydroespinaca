using AuthService.Domain.Exceptions;

namespace AuthService.Domain.Tests.Exceptions;

public class ExceptionTests
{
    [Fact]
    public void UserNotFoundException_WithEmail_ShouldContainEmailInMessage()
    {
        // Arrange
        var email = "test@example.com";

        // Act
        var exception = new UserNotFoundException(email);

        // Assert
        exception.Should().NotBeNull();
        exception.Message.Should().Contain(email);
    }

    [Fact]
    public void RoleHasAssignedUsersException_WithRoleIdAndUserCount_ShouldContainRoleIdAndCountInMessage()
    {
        // Arrange
        var roleId = "Admin";
        var userCount = 5L;

        // Act
        var exception = new RoleHasAssignedUsersException(roleId, userCount);

        // Assert
        exception.Should().NotBeNull();
        exception.Message.Should().Contain(roleId);
        exception.Message.Should().Contain(userCount.ToString());
    }

    [Fact]
    public void TokenExpiredException_ShouldHaveDefaultMessage()
    {
        // Act
        var exception = new TokenExpiredException();

        // Assert
        exception.Should().NotBeNull();
        exception.Message.Should().NotBeNullOrEmpty();
    }
}
