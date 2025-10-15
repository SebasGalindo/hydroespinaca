// No Application layer dependencies - integration tests work with HTTP
using AuthService.Test.Models;
using AuthService.Infrastructure.Services;
using AuthService.Test.Fixtures;
using AuthService.Test.Helpers;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Text;
using System.Text.Json;

namespace AuthService.Test.Integration;

[Collection("IntegrationTests")]
[Trait("Category", "Integration")]
public class DiagnosticTests : IClassFixture<IntegrationTestBase>, IAsyncLifetime, IDisposable
{
    private readonly IntegrationTestBase _factory;
    private readonly HttpClient _client;
    private readonly AuthTestHelper _authHelper;

    public DiagnosticTests(IntegrationTestBase factory)
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
    public async Task Diagnose_DataSeeding_And_Authentication()
    {
        Console.WriteLine("=== DIAGNOSTIC TEST START ===");
        
        // Clean database
        await _factory.CleanupDatabaseAsync();
        Console.WriteLine("Database cleaned");

        // Seed data manually
        using var scope = _factory.Services.GetRequiredService<IServiceScopeFactory>().CreateScope();
        var seedingService = scope.ServiceProvider.GetRequiredService<DataSeedingService>();
        
        Console.WriteLine("Starting manual data seeding...");
        await seedingService.SeedInitialDataAsync();
        Console.WriteLine("Manual data seeding completed");

        // Test admin login directly
        var adminLoginRequest = TestDataHelper.CreateLoginRequest();
        Console.WriteLine($"Testing admin login with: admin@demo.com / dF^J`c'662:W");

        var adminResponse = await _client.PostAsync("/api/auth/login",
            new StringContent(JsonSerializer.Serialize(adminLoginRequest), Encoding.UTF8, "application/json"));

        var adminContent = await adminResponse.Content.ReadAsStringAsync();
        Console.WriteLine($"Admin login response: {adminResponse.StatusCode}");
        Console.WriteLine($"Admin login content: {adminContent}");

        // Test isolated login method
        var isolatedResult = await _authHelper.TestLoginIsolatedAsync("admin@demo.com", "dF^J`c'662:W");
        Console.WriteLine($"Isolated login result: {isolatedResult}");

        // Check if validation endpoint works
        if (adminResponse.IsSuccessStatusCode && !string.IsNullOrEmpty(adminContent))
        {
            try
            {
                using var document = JsonDocument.Parse(adminContent);
                var root = document.RootElement;

                if (root.TryGetProperty("accessToken", out var accessTokenElement))
                {
                    var accessToken = accessTokenElement.GetString();
                    if (!string.IsNullOrEmpty(accessToken))
                    {
                        _client.DefaultRequestHeaders.Authorization = 
                            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

                        var validateResponse = await _client.GetAsync("/api/auth/validate");
                        Console.WriteLine($"Token validation response: {validateResponse.StatusCode}");
                        var validateContent = await validateResponse.Content.ReadAsStringAsync();
                        Console.WriteLine($"Token validation content: {validateContent}");
                    }
                    else
                    {
                        Console.WriteLine("❌ Admin login failed - empty access token");
                    }
                }
                else
                {
                    Console.WriteLine("❌ Admin login failed - no access token property");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during token validation test: {ex.Message}");
            }
        }

        Console.WriteLine("=== DIAGNOSTIC TEST END ===");

        // Assert - This test is primarily for diagnostics, but we should still verify basic functionality
        adminResponse.StatusCode.Should().Be(HttpStatusCode.OK, "Admin login should succeed after proper seeding");
    }

    [Fact]
    public async Task Diagnose_Password_Hashing_And_Verification()
    {
        Console.WriteLine("=== PASSWORD HASHING DIAGNOSTIC TEST START ===");
        
        // Clean database first
        await _factory.CleanupDatabaseAsync();
        Console.WriteLine("Database cleaned");

        // Test admin login using AuthTestHelper which will trigger detailed diagnostics
        try
        {
            var adminToken = await _authHelper.GetAdminTokenAsync();
            Console.WriteLine($"AuthTestHelper.GetAdminTokenAsync() succeeded: {adminToken != null}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"AuthTestHelper.GetAdminTokenAsync() failed: {ex.Message}");
        }

        Console.WriteLine("=== PASSWORD HASHING DIAGNOSTIC TEST END ===");
    }

    public void Dispose()
    {
        _authHelper.Dispose();
        _client.Dispose();
    }
}
