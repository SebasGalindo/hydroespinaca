using BffService.Domain.Exceptions;
using FluentAssertions;
using Xunit;

namespace BffService.Domain.Tests.Exceptions;

public class ProxyExceptionTests
{
    [Fact]
    public void Constructor_WithMessage_ShouldSetMessage()
    {
        // Arrange
        var message = "Proxy service unavailable";

        // Act
        var exception = new ProxyException(message);

        // Assert
        exception.Message.Should().Be(message);
        exception.InnerException.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithMessageAndInnerException_ShouldSetBoth()
    {
        // Arrange
        var message = "Proxy request failed";
        var innerException = new Exception("Connection timeout");

        // Act
        var exception = new ProxyException(message, innerException);

        // Assert
        exception.Message.Should().Be(message);
        exception.InnerException.Should().BeSameAs(innerException);
    }

    [Fact]
    public void Constructor_WithNullInnerException_ShouldNotThrow()
    {
        // Arrange
        var message = "Test message";

        // Act
        var action = () => new ProxyException(message, null!);

        // Assert
        action.Should().NotThrow();
    }
}

public class ServiceExceptionTests
{
    [Fact]
    public void Constructor_WithMessageAndStatusCode_ShouldSetProperties()
    {
        // Arrange
        var message = "Service error occurred";
        var statusCode = 500;

        // Act
        var exception = new ServiceException(message, statusCode);

        // Assert
        exception.Message.Should().Be(message);
        exception.StatusCode.Should().Be(statusCode);
        exception.InnerException.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithMessageStatusCodeAndInnerException_ShouldSetAllProperties()
    {
        // Arrange
        var message = "Service request failed";
        var statusCode = 503;
        var innerException = new Exception("Database connection failed");

        // Act
        var exception = new ServiceException(message, statusCode, innerException);

        // Assert
        exception.Message.Should().Be(message);
        exception.StatusCode.Should().Be(statusCode);
        exception.InnerException.Should().BeSameAs(innerException);
    }

    [Theory]
    [InlineData(400)]
    [InlineData(404)]
    [InlineData(500)]
    [InlineData(503)]
    public void Constructor_WithDifferentStatusCodes_ShouldSetCorrectly(int statusCode)
    {
        // Arrange
        var message = $"Error with status {statusCode}";

        // Act
        var exception = new ServiceException(message, statusCode);

        // Assert
        exception.StatusCode.Should().Be(statusCode);
    }

    [Fact]
    public void StatusCode_ShouldBeReadOnly()
    {
        // Arrange
        var exception = new ServiceException("Test", 500);

        // Act & Assert
        var statusCode = exception.StatusCode;
        statusCode.Should().Be(500);

        // Verify it's read-only by checking it doesn't have a setter
        var property = typeof(ServiceException).GetProperty(nameof(ServiceException.StatusCode));
        property!.CanWrite.Should().BeFalse();
    }
}

public class InvalidTokenExceptionTests
{
    [Fact]
    public void Constructor_WithMessage_ShouldSetMessage()
    {
        // Arrange
        var message = "Token is invalid or expired";

        // Act
        var exception = new InvalidTokenException(message);

        // Assert
        exception.Message.Should().Be(message);
    }

    [Theory]
    [InlineData("Invalid JWT token")]
    [InlineData("Token has expired")]
    [InlineData("Token signature invalid")]
    public void Constructor_WithDifferentMessages_ShouldSetCorrectly(string message)
    {
        // Arrange & Act
        var exception = new InvalidTokenException(message);

        // Assert
        exception.Message.Should().Be(message);
    }
}
