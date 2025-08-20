using AuthService.Test.Fixtures;
using AuthService.Test.Helpers;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using System.IdentityModel.Tokens.Jwt;
using Xunit;

namespace AuthService.Test.Integration;

[Collection("IntegrationTests")]
[Trait("Category", "Integration")]
public class TokenValidationTests : IClassFixture<IntegrationTestBase>, IAsyncLifetime
{
    private readonly IntegrationTestBase _factory;
    private readonly TestTokenHelper _testTokenHelper;

    public TokenValidationTests(IntegrationTestBase factory)
    {
        _factory = factory;
        _testTokenHelper = new TestTokenHelper(_factory.Services);
    }

    public async Task InitializeAsync()
    {
        await _factory.CleanupDatabaseAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public void TestTokenHelper_ShouldGenerateValidToken()
    {
        // Arrange & Act
        _testTokenHelper.LogJwtSettings();
        var token = _testTokenHelper.GenerateAdminToken();

        // Assert
        token.Should().NotBeNullOrEmpty();
        token.Length.Should().BeGreaterThan(100);
        
        // Validate token content
        var handler = new JwtSecurityTokenHandler();
        var jsonToken = handler.ReadJwtToken(token);
        
        Console.WriteLine($"🔍 [TokenValidationTest] Token validation:");
        Console.WriteLine($"   - Issuer in token: '{jsonToken.Issuer}'");
        Console.WriteLine($"   - Audiences in token: {string.Join(", ", jsonToken.Audiences)}");
        Console.WriteLine($"   - Subject: {jsonToken.Subject}");
        
        jsonToken.Issuer.Should().Be("test-issuer");
        jsonToken.Audiences.Should().Contain("test-audience");
    }

    [Fact]
    public void TokenService_ShouldValidateTokenCorrectly()
    {
        // Arrange
        var token = _testTokenHelper.GenerateAdminToken();
        
        // Act
        var isValid = _testTokenHelper.ValidateToken(token);
        
        // Assert
        isValid.Should().BeTrue("Token should be valid when using same key and settings");
    }
}