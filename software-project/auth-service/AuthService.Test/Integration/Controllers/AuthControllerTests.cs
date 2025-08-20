// No Application layer dependencies - integration tests work with HTTP
using AuthService.Test.Models;
using AuthService.Infrastructure.Services;
using AuthService.Test.Fixtures;
using AuthService.Test.Helpers;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace AuthService.Test.Integration.Controllers;

[Collection("IntegrationTests")]
[Trait("Category", "Integration")]
public class AuthControllerTests : IClassFixture<IntegrationTestBase>, IAsyncLifetime, IDisposable
{
    private readonly IntegrationTestBase _factory;
    private readonly HttpClient _client;
    private readonly AuthTestHelper _authHelper;

    public AuthControllerTests(IntegrationTestBase factory)
    {
        _factory = factory;
        _client = _factory.CreateFreshClient();
        _authHelper = new AuthTestHelper(_client, _factory.Services, _factory.Database);
    }

    public async Task InitializeAsync()
    {
        await _factory.CleanupDatabaseAsync();
        _authHelper.ClearAuthorizationHeader();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Login_ValidCredentials_ReturnsOkWithTokens()
    {
        // Arrange - Use centralized DataSeedingService to ensure admin user exists
        await _factory.CleanupDatabaseAsync();
        
        using var scope = _factory.Services.GetRequiredService<IServiceScopeFactory>().CreateScope();
        var seedingService = scope.ServiceProvider.GetRequiredService<DataSeedingService>();
        await seedingService.SeedInitialDataAsync();
        
        var loginRequest = TestDataHelper.CreateLoginRequest();

        // Act
        var response = await _client.PostAsync("/api/auth/login",
            new StringContent(JsonSerializer.Serialize(loginRequest), Encoding.UTF8, "application/json"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(content);
        var root = document.RootElement;

        root.TryGetProperty("accessToken", out var accessToken).Should().BeTrue();
        root.TryGetProperty("refreshToken", out var refreshToken).Should().BeTrue();
        root.TryGetProperty("expiresAt", out var expiresAt).Should().BeTrue();

        accessToken.GetString().Should().NotBeNullOrEmpty();
        refreshToken.GetString().Should().NotBeNullOrEmpty();
        expiresAt.GetDateTime().Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public async Task Login_InvalidCredentials_ReturnsUnauthorized()
    {
        // Arrange
        await _factory.CleanupDatabaseAsync();
        var loginRequest = TestDataHelper.CreateLoginRequest("invalid@example.com", "wrongpassword");

        // Act
        var response = await _client.PostAsync("/api/auth/login",
            new StringContent(JsonSerializer.Serialize(loginRequest), Encoding.UTF8, "application/json"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_InvalidEmail_ReturnsBadRequest()
    {
        // Arrange
        var loginRequest = new LoginRequestDto { Email = "invalid-email", Password = "password" };

        // Act
        var response = await _client.PostAsync("/api/auth/login",
            new StringContent(JsonSerializer.Serialize(loginRequest), Encoding.UTF8, "application/json"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_EmptyCredentials_ReturnsBadRequest()
    {
        // Arrange
        var loginRequest = new LoginRequestDto { Email = "", Password = "" };

        // Act
        var response = await _client.PostAsync("/api/auth/login",
            new StringContent(JsonSerializer.Serialize(loginRequest), Encoding.UTF8, "application/json"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Refresh_ValidRefreshToken_ReturnsOkWithNewTokens()
    {
        // Arrange
        await _factory.CleanupDatabaseAsync();
        
        using var scope = _factory.Services.GetRequiredService<IServiceScopeFactory>().CreateScope();
        var seedingService = scope.ServiceProvider.GetRequiredService<DataSeedingService>();
        await seedingService.SeedInitialDataAsync();

        // First login to get refresh token
        var loginRequest = TestDataHelper.CreateLoginRequest();
        var loginResponse = await _client.PostAsync("/api/auth/login",
            new StringContent(JsonSerializer.Serialize(loginRequest), Encoding.UTF8, "application/json"));

        var loginContent = await loginResponse.Content.ReadAsStringAsync();
        
        // Debug login response
        if (!loginResponse.IsSuccessStatusCode)
        {
            Console.WriteLine($"[TEST DEBUG] Login failed with {loginResponse.StatusCode}: {loginContent}");
        }
        else
        {
            Console.WriteLine($"[TEST DEBUG] Login succeeded: {loginContent}");
        }
        
        using var loginDocument = JsonDocument.Parse(loginContent);
        var loginRoot = loginDocument.RootElement;
        
        var refreshTokenValue = loginRoot.GetProperty("refreshToken").GetString();
        var clientIdValue = loginRoot.TryGetProperty("clientId", out var clientIdProp) ? clientIdProp.GetString() : null;

        // Now use refresh token
        var refreshRequest = new { RefreshToken = refreshTokenValue, ClientId = clientIdValue };

        // Act
        var response = await _client.PostAsync("/api/auth/refresh",
            new StringContent(JsonSerializer.Serialize(refreshRequest), Encoding.UTF8, "application/json"));

        // Debug: Log response details if not successful
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"[TEST DEBUG] Refresh failed with {response.StatusCode}: {errorContent}");
            Console.WriteLine($"[TEST DEBUG] Login response ClientId: '{clientIdValue}'");
            Console.WriteLine($"[TEST DEBUG] Refresh token length: {refreshTokenValue?.Length ?? 0}");
        }

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(content);
        var root = document.RootElement;

        root.TryGetProperty("accessToken", out var accessToken).Should().BeTrue();
        root.TryGetProperty("refreshToken", out var refreshToken).Should().BeTrue();

        var newAccessToken = accessToken.GetString();
        var newRefreshToken = refreshToken.GetString();
        
        newAccessToken.Should().NotBeNullOrEmpty();
        newRefreshToken.Should().NotBeNullOrEmpty();
        // New tokens should be different
        newAccessToken.Should().NotBe(loginRoot.GetProperty("accessToken").GetString());
        newRefreshToken.Should().NotBe(refreshTokenValue);
    }

    [Fact]
    public async Task Refresh_InvalidRefreshToken_ReturnsUnauthorized()
    {
        // Arrange
        var refreshRequest = new RefreshRequestDto 
        { 
            RefreshToken = "invalid_refresh_token",
            ClientId = "test-client"
        };

        // Act
        var response = await _client.PostAsync("/api/auth/refresh",
            new StringContent(JsonSerializer.Serialize(refreshRequest), Encoding.UTF8, "application/json"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Validate_ValidToken_ReturnsOk()
    {
        // Arrange
        await _factory.CleanupDatabaseAsync();
        var token = await _authHelper.GetAdminTokenAsync();
        _authHelper.SetAuthorizationHeader(token);

        // Act
        var response = await _client.GetAsync("/api/auth/validate");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<bool>(content);
        result.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_NoToken_ReturnsUnauthorized()
    {
        // Arrange
        _authHelper.ClearAuthorizationHeader();

        // Act
        var response = await _client.GetAsync("/api/auth/validate");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Validate_InvalidToken_ReturnsUnauthorized()
    {
        // Arrange
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "invalid_token");

        // Act
        var response = await _client.GetAsync("/api/auth/validate");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PublicKey_ReturnsOkWithJwks()
    {
        // Act
        var response = await _client.GetAsync("/api/auth/keys/public");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrEmpty();
        
        // Should return JWKS format with both user and M2M keys
        content.Should().Contain("\"keys\"");
        content.Should().Contain("\"kty\":\"RSA\"");
        content.Should().Contain("\"use\":\"sig\"");
        content.Should().Contain("\"alg\":\"RS256\"");
        content.Should().Contain("test-user-"); // User token key ID
        content.Should().Contain("test-machinetomachine-"); // M2M token key ID
    }

    public void Dispose()
    {
        _authHelper.Dispose();
        _client.Dispose();
    }
}