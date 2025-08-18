using AuthService.Domain.Enums;
using AuthService.Domain.Interfaces;
using AuthService.Test.Fixtures;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using System.IdentityModel.Tokens.Jwt;
using Xunit;

namespace AuthService.Test.Integration;

[Collection("IntegrationTests")]
[Trait("Category", "Integration")]
public class RoleClaimTests : IClassFixture<IntegrationTestBase>, IDisposable
{
    private readonly HttpClient _client;
    private readonly IntegrationTestBase _testBase;

    public RoleClaimTests(IntegrationTestBase testBase)
    {
        _testBase = testBase;
        _client = testBase.CreateClient();
    }

    [Fact]
    public void UserToken_ShouldHaveSimpleRoleClaim()
    {
        // Arrange
        var tokenService = _testBase.Services.GetRequiredService<ITokenService>();
        
        // Act - Generate a user token
        var tokenResult = tokenService.GenerateTokens(
            userId: "test-user-123",
            email: "test@example.com", 
            role: "role_admin",
            clientId: null,
            tokenType: TokenType.User);

        // Assert - Verify token has simple "role" claim
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(tokenResult.AccessToken);
        
        // Should have simple "role" claim, not Microsoft's namespace
        var roleClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "role");
        roleClaim.Should().NotBeNull();
        roleClaim!.Value.Should().Be("role_admin");
        
        // Should NOT have Microsoft's role claim type
        var microsoftRoleClaim = jwtToken.Claims.FirstOrDefault(c => 
            c.Type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role");
        microsoftRoleClaim.Should().BeNull();
    }

    [Fact]
    public void M2MToken_ShouldHaveSimpleRoleClaim()
    {
        // Arrange
        var tokenService = _testBase.Services.GetRequiredService<ITokenService>();
        
        // Act - Generate an M2M token
        var tokenResult = tokenService.GenerateTokens(
            userId: "test-client-123",
            email: "client@example.com",
            role: "client", 
            clientId: "test-client",
            tokenType: TokenType.MachineToMachine);

        // Assert - Verify token has simple "role" claim
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(tokenResult.AccessToken);
        
        // Should have simple "role" claim
        var roleClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "role");
        roleClaim.Should().NotBeNull();
        roleClaim!.Value.Should().Be("client");
        
        // Should NOT have Microsoft's role claim type
        var microsoftRoleClaim = jwtToken.Claims.FirstOrDefault(c => 
            c.Type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role");
        microsoftRoleClaim.Should().BeNull();
    }

    [Fact]
    public void TokenClaims_ShouldHaveExpectedStructure()
    {
        // Arrange
        var tokenService = _testBase.Services.GetRequiredService<ITokenService>();
        
        // Act - Generate a token
        var tokenResult = tokenService.GenerateTokens(
            userId: "user-123",
            email: "user@test.com", 
            role: "role_user",
            clientId: "test-client",
            tokenType: TokenType.User);

        // Assert - Verify token structure
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(tokenResult.AccessToken);
        
        // Expected claims with simple names
        var claims = jwtToken.Claims.ToDictionary(c => c.Type, c => c.Value);
        
        claims.Should().ContainKey("sub").WhoseValue.Should().Be("user-123");
        claims.Should().ContainKey("email").WhoseValue.Should().Be("user@test.com");
        claims.Should().ContainKey("role").WhoseValue.Should().Be("role_user");
        claims.Should().ContainKey("client_id").WhoseValue.Should().Be("test-client");
        claims.Should().ContainKey("jti");
        claims.Should().ContainKey("nbf");
        claims.Should().ContainKey("exp");
        claims.Should().ContainKey("iss");
        claims.Should().ContainKey("aud");
        
        // Should NOT contain Microsoft namespace claims
        claims.Keys.Should().NotContain(k => k.StartsWith("http://schemas.microsoft.com/"));
        claims.Keys.Should().NotContain(k => k.StartsWith("http://schemas.xmlsoap.org/"));
    }

    public void Dispose()
    {
        _client?.Dispose();
    }
}