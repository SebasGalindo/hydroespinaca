// No Application layer dependencies - integration tests work with HTTP  
using AuthService.Test.Models;
using AuthService.Test.Fixtures;
using AuthService.Test.Helpers;
using FluentAssertions;
using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Json;

namespace AuthService.Test.Integration.Controllers;

[Collection("IntegrationTests")]
[Trait("Category", "Integration")]
public class PermissionControllerTests : IClassFixture<IntegrationTestBase>, IAsyncLifetime, IDisposable
{
    private readonly IntegrationTestBase _factory;
    private readonly HttpClient _client;
    private readonly AuthTestHelper _authHelper;

    public PermissionControllerTests(IntegrationTestBase factory)
    {
        _factory = factory;
        _client = _factory.CreateFreshClient();
        _authHelper = new AuthTestHelper(_client, _factory.Services, _factory.Database);
    }

    public async Task InitializeAsync()
    {
        // Cleanup before each test class
        await _factory.CleanupDatabaseAsync();
        _authHelper.ClearAuthorizationHeader();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Create_WithAdminRole_ReturnsCreated()
    {
        // Arrange
        await _factory.CleanupDatabaseAsync();
        _authHelper.ClearAuthorizationHeader(); // Ensure clean state
        var token = await _authHelper.GetAdminTokenAsync();
        _authHelper.SetAuthorizationHeader(token);

        var request = TestDataHelper.CreatePermissionRequest("perm_test_create", "Test Create Permission");

        // Act
        var response = await _client.PostAsync("/api/permissions",
            new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var content = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(content);
        var root = document.RootElement;

        root.TryGetProperty("code", out var code).Should().BeTrue();
        root.TryGetProperty("name", out var name).Should().BeTrue();
        root.TryGetProperty("description", out var description).Should().BeTrue();
        root.TryGetProperty("id", out var id).Should().BeTrue();

        code.GetString().Should().Be("perm_test_create");
        name.GetString().Should().Be("Test Create Permission");
        description.GetString().Should().NotBeNullOrEmpty();
        id.GetString().Should().NotBeNullOrEmpty();

        // Verify Location header
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.ToString().Should().Contain("/api/permissions/perm_test_create");
    }

    [Fact]
    public async Task Create_WithUserRole_ReturnsForbidden()
    {
        // Arrange
        await _factory.CleanupDatabaseAsync();
        _authHelper.ClearAuthorizationHeader(); // Ensure clean state
        var token = await _authHelper.GetUserTokenAsync();
        _authHelper.SetAuthorizationHeader(token);

        var request = TestDataHelper.CreatePermissionRequest("perm_test_forbidden", "Test Forbidden Permission");

        // Act
        var response = await _client.PostAsync("/api/permissions",
            new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Create_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange
        _authHelper.ClearAuthorizationHeader();
        var request = TestDataHelper.CreatePermissionRequest("perm_test_unauth", "Test Unauthorized Permission");

        // Act
        var response = await _client.PostAsync("/api/permissions",
            new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Create_InvalidCode_ReturnsBadRequest()
    {
        // Arrange
        await _factory.CleanupDatabaseAsync();
        _authHelper.ClearAuthorizationHeader(); // Ensure clean state
        var token = await _authHelper.GetAdminTokenAsync();
        _authHelper.SetAuthorizationHeader(token);

        var request = TestDataHelper.CreatePermissionRequest("invalid_code", "Test Invalid Permission");

        // Act
        var response = await _client.PostAsync("/api/permissions",
            new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("El código del permiso debe comenzar con 'perm_'");
    }

    [Fact]
    public async Task Create_DuplicateCode_ReturnsConflict()
    {
        // Arrange
        await _factory.CleanupDatabaseAsync();
        _authHelper.ClearAuthorizationHeader(); // Ensure clean state
        var token = await _authHelper.GetAdminTokenAsync();
        _authHelper.SetAuthorizationHeader(token);

        var request = TestDataHelper.CreatePermissionRequest("perm_test_duplicate", "Test Duplicate Permission");

        // Create first permission
        await _client.PostAsync("/api/permissions",
            new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

        // Act - Try to create duplicate
        var response = await _client.PostAsync("/api/permissions",
            new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problem.Title.Should().Be("Invalid Parameter");
        problem.Status.Should().Be(400);

    }

    [Fact]
    public async Task GetByCode_ExistingPermission_ReturnsOk()
    {
        // Arrange
        await _factory.CleanupDatabaseAsync();
        _authHelper.ClearAuthorizationHeader(); // Ensure clean state
        var token = await _authHelper.GetAdminTokenAsync();
        _authHelper.SetAuthorizationHeader(token);

        var request = TestDataHelper.CreatePermissionRequest("perm_test_get", "Test Get Permission");

        // Create permission first
        await _client.PostAsync("/api/permissions",
            new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

        // Act
        var response = await _client.GetAsync($"/api/permissions/{request.Code}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<PermissionResponseDto>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        result.Should().NotBeNull();
        result!.Code.Should().Be(request.Code);
        result.Name.Should().Be(request.Name);
    }

    [Fact]
    public async Task GetByCode_NonExistingPermission_ReturnsNotFound()
    {
        // Arrange
        await _factory.CleanupDatabaseAsync();
        _authHelper.ClearAuthorizationHeader(); // Ensure clean state
        var token = await _authHelper.GetAdminTokenAsync();
        _authHelper.SetAuthorizationHeader(token);

        // Act
        var response = await _client.GetAsync("/api/permissions/perm_non_existing");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetAll_WithAuthentication_ReturnsOkWithPermissions()
    {
        // Arrange
        await _factory.CleanupDatabaseAsync();
        _authHelper.ClearAuthorizationHeader(); // Ensure clean state
        var token = await _authHelper.GetAdminTokenAsync();
        _authHelper.SetAuthorizationHeader(token);

        // Create test permissions
        var request1 = TestDataHelper.CreatePermissionRequest("perm_test_getall_1", "Test GetAll Permission 1");
        var request2 = TestDataHelper.CreatePermissionRequest("perm_test_getall_2", "Test GetAll Permission 2");

        await _client.PostAsync("/api/permissions",
            new StringContent(JsonSerializer.Serialize(request1), Encoding.UTF8, "application/json"));
        await _client.PostAsync("/api/permissions",
            new StringContent(JsonSerializer.Serialize(request2), Encoding.UTF8, "application/json"));

        // Act
        var response = await _client.GetAsync("/api/permissions");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<List<PermissionResponseDto>>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        result.Should().NotBeNull();
        result!.Should().HaveCountGreaterOrEqualTo(2);
        result.Should().Contain(p => p.Code == request1.Code);
        result.Should().Contain(p => p.Code == request2.Code);
    }

    [Fact]
    public async Task Update_ExistingPermission_ReturnsOk()
    {
        // Arrange
        await _factory.CleanupDatabaseAsync();
        _authHelper.ClearAuthorizationHeader(); // Ensure clean state
        var token = await _authHelper.GetAdminTokenAsync();
        _authHelper.SetAuthorizationHeader(token);

        var createRequest = TestDataHelper.CreatePermissionRequest("perm_test_update", "Original Name");

        // Create permission first
        var createResponse = await _client.PostAsync(
            "/api/permissions",
            new StringContent(JsonSerializer.Serialize(createRequest), Encoding.UTF8, "application/json")
        );

        createResponse.EnsureSuccessStatusCode();

        var updateRequest = TestDataHelper.UpdatePermissionRequest("Updated Name");
        // Act
        var response = await _client.PutAsync(
            $"/api/permissions/{createRequest.Code}",
            new StringContent(JsonSerializer.Serialize(updateRequest), Encoding.UTF8, "application/json")
        );




        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<PermissionResponseDto>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        result.Should().NotBeNull();
        result!.Code.Should().Be(createRequest.Code);
        result.Name.Should().Be(updateRequest.Name);
        result.Description.Should().Be(updateRequest.Description);
    }

    [Fact]
    public async Task Update_NonExistingPermission_ReturnsNotFound()
    {
        // Arrange
        await _factory.CleanupDatabaseAsync();
        _authHelper.ClearAuthorizationHeader(); // Ensure clean state
        var token = await _authHelper.GetAdminTokenAsync();
        _authHelper.SetAuthorizationHeader(token);

        var updateRequest = TestDataHelper.UpdatePermissionRequest("Updated Name");

        // Act
        var response = await _client.PutAsync("/api/permissions/perm_non_existing",
            new StringContent(JsonSerializer.Serialize(updateRequest), Encoding.UTF8, "application/json"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_ExistingPermission_ReturnsNoContent()
    {
        // Arrange
        await _factory.CleanupDatabaseAsync();
        _authHelper.ClearAuthorizationHeader(); // Ensure clean state
        var token = await _authHelper.GetAdminTokenAsync();
        _authHelper.SetAuthorizationHeader(token);

        var request = TestDataHelper.CreatePermissionRequest("perm_test_delete", "Test Delete Permission");

        // Create permission first
        await _client.PostAsync("/api/permissions",
            new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

        // Act
        var response = await _client.DeleteAsync($"/api/permissions/{request.Code}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify it's deleted
        var getResponse = await _client.GetAsync($"/api/permissions/{request.Code}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_NonExistingPermission_ReturnsNotFound()
    {
        // Arrange
        await _factory.CleanupDatabaseAsync();
        _authHelper.ClearAuthorizationHeader(); // Ensure clean state
        var token = await _authHelper.GetAdminTokenAsync();
        _authHelper.SetAuthorizationHeader(token);

        // Act
        var response = await _client.DeleteAsync("/api/permissions/perm_non_existing");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    public void Dispose()
    {
        _authHelper.Dispose();
        _client.Dispose();
    }
}