using BffService.Application.DTOs;
using FluentAssertions;
using Xunit;

namespace BffService.Application.Tests.DTOs;

public class UserSessionDtoTests
{
    [Fact]
    public void Constructor_ShouldSetAllProperties()
    {
        // Arrange
        var userId = "user123";
        var sessionId = "session456";
        var username = "testuser";
        var email = "test@example.com";
        var role = "Admin";

        // Act
        var dto = new UserSessionDto(userId, sessionId, username, email, role);

        // Assert
        dto.UserId.Should().Be(userId);
        dto.SessionId.Should().Be(sessionId);
        dto.Username.Should().Be(username);
        dto.Email.Should().Be(email);
        dto.Role.Should().Be(role);
    }

    [Fact]
    public void Equals_WithSameValues_ShouldReturnTrue()
    {
        // Arrange
        var dto1 = new UserSessionDto("user1", "session1", "testuser", "test@example.com", "Admin");
        var dto2 = new UserSessionDto("user1", "session1", "testuser", "test@example.com", "Admin");

        // Act & Assert
        dto1.Should().Be(dto2);
    }

    [Fact]
    public void Equals_WithDifferentUserId_ShouldReturnFalse()
    {
        // Arrange
        var dto1 = new UserSessionDto("user1", "session1", "testuser", "test@example.com", "Admin");
        var dto2 = new UserSessionDto("user2", "session1", "testuser", "test@example.com", "Admin");

        // Act & Assert
        dto1.Should().NotBe(dto2);
    }

    [Fact]
    public void WithExpression_ShouldCreateNewInstanceWithUpdatedProperty()
    {
        // Arrange
        var original = new UserSessionDto("user1", "session1", "testuser", "test@example.com", "User");

        // Act
        var updated = original with { Role = "Admin" };

        // Assert
        updated.Role.Should().Be("Admin");
        updated.UserId.Should().Be(original.UserId);
        original.Role.Should().Be("User"); // Original unchanged
    }

    [Fact]
    public void Deconstruct_ShouldExtractAllProperties()
    {
        // Arrange
        var dto = new UserSessionDto("user123", "session456", "testuser", "test@example.com", "Admin");

        // Act
        var (userId, sessionId, username, email, role, hasAcceptedTerms) = dto;

        // Assert
        userId.Should().Be("user123");
        sessionId.Should().Be("session456");
        username.Should().Be("testuser");
        email.Should().Be("test@example.com");
        role.Should().Be("Admin");
        hasAcceptedTerms.Should().BeFalse();
    }
}

public class WebLoginResponseDtoTests
{
    [Fact]
    public void Constructor_ShouldSetAllProperties()
    {
        // Arrange
        var message = "Login successful";
        var sessionId = "session123";
        var csrfToken = "csrf456";

        // Act
        var dto = new WebLoginResponseDto(message, sessionId, csrfToken);

        // Assert
        dto.Message.Should().Be(message);
        dto.SessionId.Should().Be(sessionId);
        dto.CsrfToken.Should().Be(csrfToken);
    }

    [Fact]
    public void Constructor_WithDefaultValues_ShouldUseDefaults()
    {
        // Act
        var dto = new WebLoginResponseDto();

        // Assert
        dto.Message.Should().Be("Login successful");
        dto.SessionId.Should().BeNull();
        dto.CsrfToken.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithPartialValues_ShouldSetCorrectly()
    {
        // Act
        var dto = new WebLoginResponseDto("Custom message");

        // Assert
        dto.Message.Should().Be("Custom message");
        dto.SessionId.Should().BeNull();
        dto.CsrfToken.Should().BeNull();
    }

    [Fact]
    public void Equals_WithSameValues_ShouldReturnTrue()
    {
        // Arrange
        var dto1 = new WebLoginResponseDto("Success", "session1", "csrf1");
        var dto2 = new WebLoginResponseDto("Success", "session1", "csrf1");

        // Act & Assert
        dto1.Should().Be(dto2);
    }

    [Fact]
    public void Equals_WithDifferentValues_ShouldReturnFalse()
    {
        // Arrange
        var dto1 = new WebLoginResponseDto("Success", "session1", "csrf1");
        var dto2 = new WebLoginResponseDto("Success", "session2", "csrf2");

        // Act & Assert
        dto1.Should().NotBe(dto2);
    }

    [Fact]
    public void WithExpression_ShouldCreateNewInstanceWithUpdatedProperty()
    {
        // Arrange
        var original = new WebLoginResponseDto("Original", null, null);

        // Act
        var updated = original with { SessionId = "new_session", CsrfToken = "new_csrf" };

        // Assert
        updated.SessionId.Should().Be("new_session");
        updated.CsrfToken.Should().Be("new_csrf");
        updated.Message.Should().Be("Original");
        original.SessionId.Should().BeNull(); // Original unchanged
    }

    [Fact]
    public void Deconstruct_ShouldExtractAllProperties()
    {
        // Arrange
        var dto = new WebLoginResponseDto("Success", "session123", "csrf456");

        // Act
        var (message, sessionId, csrfToken) = dto;

        // Assert
        message.Should().Be("Success");
        sessionId.Should().Be("session123");
        csrfToken.Should().Be("csrf456");
    }
}

public class MobileLoginResponseDtoTests
{
    [Fact]
    public void Constructor_ShouldSetAllProperties()
    {
        // Arrange
        var sessionId = "session123";
        var csrfToken = "csrf456";
        var message = "Login successful";

        // Act
        var dto = new MobileLoginResponseDto(sessionId, csrfToken, message);

        // Assert
        dto.SessionId.Should().Be(sessionId);
        dto.CsrfToken.Should().Be(csrfToken);
        dto.Message.Should().Be(message);
    }

    [Fact]
    public void Constructor_WithDefaultMessage_ShouldUseDefault()
    {
        // Arrange
        var sessionId = "session123";
        var csrfToken = "csrf456";

        // Act
        var dto = new MobileLoginResponseDto(sessionId, csrfToken);

        // Assert
        dto.SessionId.Should().Be(sessionId);
        dto.CsrfToken.Should().Be(csrfToken);
        dto.Message.Should().Be("Login successful");
    }

    [Fact]
    public void Constructor_WithCustomMessage_ShouldSetCorrectly()
    {
        // Arrange
        var sessionId = "session123";
        var csrfToken = "csrf456";
        var customMessage = "Welcome back!";

        // Act
        var dto = new MobileLoginResponseDto(sessionId, csrfToken, customMessage);

        // Assert
        dto.Message.Should().Be(customMessage);
    }

    [Fact]
    public void Equals_WithSameValues_ShouldReturnTrue()
    {
        // Arrange
        var dto1 = new MobileLoginResponseDto("session1", "csrf1", "Success");
        var dto2 = new MobileLoginResponseDto("session1", "csrf1", "Success");

        // Act & Assert
        dto1.Should().Be(dto2);
    }

    [Fact]
    public void Equals_WithDifferentSessionId_ShouldReturnFalse()
    {
        // Arrange
        var dto1 = new MobileLoginResponseDto("session1", "csrf1", "Success");
        var dto2 = new MobileLoginResponseDto("session2", "csrf1", "Success");

        // Act & Assert
        dto1.Should().NotBe(dto2);
    }

    [Fact]
    public void WithExpression_ShouldCreateNewInstanceWithUpdatedProperty()
    {
        // Arrange
        var original = new MobileLoginResponseDto("old_session", "old_csrf", "Original");

        // Act
        var updated = original with { Message = "Updated message" };

        // Assert
        updated.Message.Should().Be("Updated message");
        updated.SessionId.Should().Be(original.SessionId);
        updated.CsrfToken.Should().Be(original.CsrfToken);
        original.Message.Should().Be("Original"); // Original unchanged
    }

    [Fact]
    public void Deconstruct_ShouldExtractAllProperties()
    {
        // Arrange
        var dto = new MobileLoginResponseDto("session123", "csrf456", "Success");

        // Act
        var (sessionId, csrfToken, message) = dto;

        // Assert
        sessionId.Should().Be("session123");
        csrfToken.Should().Be("csrf456");
        message.Should().Be("Success");
    }

    [Fact]
    public void Deconstruct_WithDefaultMessage_ShouldExtractCorrectly()
    {
        // Arrange
        var dto = new MobileLoginResponseDto("session123", "csrf456");

        // Act
        var (sessionId, csrfToken, message) = dto;

        // Assert
        sessionId.Should().Be("session123");
        csrfToken.Should().Be("csrf456");
        message.Should().Be("Login successful");
    }
}
