using BffService.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace BffService.Domain.Tests.ValueObjects;

public class TokenInfoTests
{
    [Fact]
    public void Constructor_ShouldSetAllProperties()
    {
        // Arrange
        var accessToken = "access_token_123";
        var refreshToken = "refresh_token_456";
        var expiresAt = DateTime.UtcNow.AddHours(1);
        var refreshTokenExpiresAt = DateTime.UtcNow.AddDays(7);

        // Act
        var tokenInfo = new TokenInfo(accessToken, refreshToken, expiresAt, refreshTokenExpiresAt);

        // Assert
        tokenInfo.AccessToken.Should().Be(accessToken);
        tokenInfo.RefreshToken.Should().Be(refreshToken);
        tokenInfo.ExpiresAt.Should().Be(expiresAt);
        tokenInfo.RefreshTokenExpiresAt.Should().Be(refreshTokenExpiresAt);
    }

    [Fact]
    public void Constructor_WithoutRefreshTokenExpiration_ShouldSetToNull()
    {
        // Arrange
        var accessToken = "access_token_123";
        var refreshToken = "refresh_token_456";
        var expiresAt = DateTime.UtcNow.AddHours(1);

        // Act
        var tokenInfo = new TokenInfo(accessToken, refreshToken, expiresAt);

        // Assert
        tokenInfo.AccessToken.Should().Be(accessToken);
        tokenInfo.RefreshToken.Should().Be(refreshToken);
        tokenInfo.ExpiresAt.Should().Be(expiresAt);
        tokenInfo.RefreshTokenExpiresAt.Should().BeNull();
    }

    [Fact]
    public void Equals_WithSameValues_ShouldReturnTrue()
    {
        // Arrange
        var expiresAt = DateTime.UtcNow.AddHours(1);
        var refreshExpiresAt = DateTime.UtcNow.AddDays(7);
        var tokenInfo1 = new TokenInfo("token1", "refresh1", expiresAt, refreshExpiresAt);
        var tokenInfo2 = new TokenInfo("token1", "refresh1", expiresAt, refreshExpiresAt);

        // Act & Assert
        tokenInfo1.Should().Be(tokenInfo2);
    }

    [Fact]
    public void Equals_WithDifferentValues_ShouldReturnFalse()
    {
        // Arrange
        var expiresAt = DateTime.UtcNow.AddHours(1);
        var tokenInfo1 = new TokenInfo("token1", "refresh1", expiresAt);
        var tokenInfo2 = new TokenInfo("token2", "refresh2", expiresAt);

        // Act & Assert
        tokenInfo1.Should().NotBe(tokenInfo2);
    }

    [Fact]
    public void GetHashCode_WithSameValues_ShouldReturnSameHash()
    {
        // Arrange
        var expiresAt = DateTime.UtcNow.AddHours(1);
        var tokenInfo1 = new TokenInfo("token1", "refresh1", expiresAt);
        var tokenInfo2 = new TokenInfo("token1", "refresh1", expiresAt);

        // Act & Assert
        tokenInfo1.GetHashCode().Should().Be(tokenInfo2.GetHashCode());
    }
}

public class AuthenticationResultTests
{
    [Fact]
    public void Constructor_ShouldSetAllProperties()
    {
        // Arrange
        var tokenInfo = new TokenInfo("access", "refresh", DateTime.UtcNow.AddHours(1));
        var userId = "user123";
        var username = "testuser";
        var email = "test@example.com";
        var userRole = "Admin";
        var scopes = new List<string> { "read", "write" };

        // Act
        var result = new AuthenticationResult(tokenInfo, userId, username, email, userRole, scopes);

        // Assert
        result.TokenInfo.Should().Be(tokenInfo);
        result.UserId.Should().Be(userId);
        result.Username.Should().Be(username);
        result.Email.Should().Be(email);
        result.UserRole.Should().Be(userRole);
        result.Scopes.Should().BeEquivalentTo(scopes);
    }

    [Fact]
    public void Equals_WithSameValues_ShouldReturnTrue()
    {
        // Arrange
        var tokenInfo = new TokenInfo("access", "refresh", DateTime.UtcNow.AddHours(1));
        var scopes = new List<string> { "read", "write" };
        var result1 = new AuthenticationResult(tokenInfo, "user1", "testuser", "test@example.com", "Admin", scopes);
        var result2 = new AuthenticationResult(tokenInfo, "user1", "testuser", "test@example.com", "Admin", scopes);

        // Act & Assert
        result1.Should().Be(result2);
    }

    [Fact]
    public void Equals_WithDifferentUserId_ShouldReturnFalse()
    {
        // Arrange
        var tokenInfo = new TokenInfo("access", "refresh", DateTime.UtcNow.AddHours(1));
        var scopes = new List<string> { "read", "write" };
        var result1 = new AuthenticationResult(tokenInfo, "user1", "testuser", "test@example.com", "Admin", scopes);
        var result2 = new AuthenticationResult(tokenInfo, "user2", "testuser", "test@example.com", "Admin", scopes);

        // Act & Assert
        result1.Should().NotBe(result2);
    }

    [Fact]
    public void WithExpression_ShouldCreateNewInstanceWithUpdatedProperty()
    {
        // Arrange
        var tokenInfo = new TokenInfo("access", "refresh", DateTime.UtcNow.AddHours(1));
        var scopes = new List<string> { "read" };
        var original = new AuthenticationResult(tokenInfo, "user1", "testuser", "test@example.com", "User", scopes);

        // Act
        var updated = original with { UserRole = "Admin" };

        // Assert
        updated.UserRole.Should().Be("Admin");
        updated.UserId.Should().Be(original.UserId);
        updated.Username.Should().Be(original.Username);
        original.UserRole.Should().Be("User"); // Original unchanged
    }
}

public class ProxyRequestTests
{
    [Fact]
    public void Constructor_ShouldSetAllProperties()
    {
        // Arrange
        var method = "POST";
        var path = "/api/data";
        var headers = new Dictionary<string, string> { { "Content-Type", "application/json" } };
        var body = "{\"key\":\"value\"}";

        // Act
        var request = new ProxyRequest(method, path, headers, body);

        // Assert
        request.Method.Should().Be(method);
        request.Path.Should().Be(path);
        request.Headers.Should().BeEquivalentTo(headers);
        request.Body.Should().Be(body);
    }

    [Fact]
    public void Constructor_WithoutBody_ShouldSetBodyToNull()
    {
        // Arrange
        var method = "GET";
        var path = "/api/data";
        var headers = new Dictionary<string, string> { { "Authorization", "Bearer token" } };

        // Act
        var request = new ProxyRequest(method, path, headers);

        // Assert
        request.Method.Should().Be(method);
        request.Path.Should().Be(path);
        request.Headers.Should().BeEquivalentTo(headers);
        request.Body.Should().BeNull();
    }

    [Fact]
    public void Equals_WithSameValues_ShouldReturnTrue()
    {
        // Arrange
        var headers = new Dictionary<string, string> { { "Key", "Value" } };
        var request1 = new ProxyRequest("POST", "/api/test", headers, "body");
        var request2 = new ProxyRequest("POST", "/api/test", headers, "body");

        // Act & Assert
        request1.Should().Be(request2);
    }

    [Fact]
    public void Equals_WithDifferentPath_ShouldReturnFalse()
    {
        // Arrange
        var headers = new Dictionary<string, string>();
        var request1 = new ProxyRequest("GET", "/api/test1", headers);
        var request2 = new ProxyRequest("GET", "/api/test2", headers);

        // Act & Assert
        request1.Should().NotBe(request2);
    }

    [Fact]
    public void WithExpression_ShouldCreateNewInstanceWithUpdatedProperty()
    {
        // Arrange
        var headers = new Dictionary<string, string>();
        var original = new ProxyRequest("GET", "/api/old", headers);

        // Act
        var updated = original with { Path = "/api/new" };

        // Assert
        updated.Path.Should().Be("/api/new");
        updated.Method.Should().Be(original.Method);
        original.Path.Should().Be("/api/old"); // Original unchanged
    }
}

public class ProxyResponseTests
{
    [Fact]
    public void Constructor_ShouldSetAllProperties()
    {
        // Arrange
        var statusCode = 200;
        var headers = new Dictionary<string, string> { { "Content-Type", "application/json" } };
        var body = "{\"result\":\"success\"}";

        // Act
        var response = new ProxyResponse(statusCode, headers, body);

        // Assert
        response.StatusCode.Should().Be(statusCode);
        response.Headers.Should().BeEquivalentTo(headers);
        response.Body.Should().Be(body);
    }

    [Fact]
    public void Constructor_WithoutBody_ShouldSetBodyToNull()
    {
        // Arrange
        var statusCode = 204;
        var headers = new Dictionary<string, string>();

        // Act
        var response = new ProxyResponse(statusCode, headers);

        // Assert
        response.StatusCode.Should().Be(statusCode);
        response.Headers.Should().BeEquivalentTo(headers);
        response.Body.Should().BeNull();
    }

    [Theory]
    [InlineData(200)]
    [InlineData(404)]
    [InlineData(500)]
    public void Constructor_WithDifferentStatusCodes_ShouldSetCorrectly(int statusCode)
    {
        // Arrange
        var headers = new Dictionary<string, string>();

        // Act
        var response = new ProxyResponse(statusCode, headers);

        // Assert
        response.StatusCode.Should().Be(statusCode);
    }

    [Fact]
    public void Equals_WithSameValues_ShouldReturnTrue()
    {
        // Arrange
        var headers = new Dictionary<string, string> { { "Key", "Value" } };
        var response1 = new ProxyResponse(200, headers, "body");
        var response2 = new ProxyResponse(200, headers, "body");

        // Act & Assert
        response1.Should().Be(response2);
    }

    [Fact]
    public void Equals_WithDifferentStatusCode_ShouldReturnFalse()
    {
        // Arrange
        var headers = new Dictionary<string, string>();
        var response1 = new ProxyResponse(200, headers);
        var response2 = new ProxyResponse(404, headers);

        // Act & Assert
        response1.Should().NotBe(response2);
    }

    [Fact]
    public void WithExpression_ShouldCreateNewInstanceWithUpdatedProperty()
    {
        // Arrange
        var headers = new Dictionary<string, string>();
        var original = new ProxyResponse(200, headers, "original body");

        // Act
        var updated = original with { Body = "updated body" };

        // Assert
        updated.Body.Should().Be("updated body");
        updated.StatusCode.Should().Be(original.StatusCode);
        original.Body.Should().Be("original body"); // Original unchanged
    }
}
