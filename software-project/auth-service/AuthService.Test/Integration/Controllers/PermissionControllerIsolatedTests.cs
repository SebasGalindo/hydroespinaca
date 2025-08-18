// No Application layer dependencies - integration tests work with HTTP
using AuthService.Test.Models;
using AuthService.Test.Fixtures;
using AuthService.Test.Helpers;
using FluentAssertions;
using System.Net;

namespace AuthService.Test.Integration.Controllers;

/// <summary>
/// Example of how to write isolated tests that don't share state
/// </summary>
[Collection("IntegrationTests")]
[Trait("Category", "Integration")]
public class PermissionControllerIsolatedTests : IClassFixture<IntegrationTestBase>, IAsyncLifetime, IDisposable
{
    private readonly IntegrationTestBase _factory;
    private readonly IsolatedTestHelper _testHelper;

    public PermissionControllerIsolatedTests(IntegrationTestBase factory)
    {
        _factory = factory;
        _testHelper = new IsolatedTestHelper(factory);
    }

    public async Task InitializeAsync()
    {
        await _factory.CleanupDatabaseAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task CreatePermission_WithUniqueData_ShouldSucceed()
    {
        // Arrange - Each test gets fresh client and unique data
        using var client = await _testHelper.CreateAuthenticatedClientAsync(isAdmin: true);
        var request = TestDataHelper.CreatePermissionRequest(); // This now generates unique IDs

        // Act
        var response = await _testHelper.PostAsJsonAsync<PermissionResponseDto>(
            client, "/api/permissions", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Data.Should().NotBeNull();
        response.Data!.Code.Should().Be(request.Code);
    }

    [Fact]
    public async Task CreatePermission_WithUserRole_ShouldBeForbidden()
    {
        // Arrange - Fresh client with user role
        using var client = await _testHelper.CreateAuthenticatedClientAsync(isAdmin: false);
        var request = TestDataHelper.CreatePermissionRequest();

        // Act
        var response = await _testHelper.PostAsJsonAsync<PermissionResponseDto>(
            client, "/api/permissions", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreatePermission_Unauthenticated_ShouldBeUnauthorized()
    {
        // Arrange - Fresh unauthenticated client
        using var client = _testHelper.CreateUnauthenticatedClient();
        var request = TestDataHelper.CreatePermissionRequest();

        // Act
        var response = await _testHelper.PostAsJsonAsync<PermissionResponseDto>(
            client, "/api/permissions", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    public void Dispose()
    {
        _testHelper.Dispose();
    }
}