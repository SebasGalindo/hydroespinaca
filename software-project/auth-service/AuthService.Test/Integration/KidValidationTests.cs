using AuthService.Domain.Enums;
using AuthService.Domain.Interfaces;
using AuthService.Infrastructure.Security;
using AuthService.Test.Fixtures;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using Xunit;

namespace AuthService.Test.Integration;

[Collection("IntegrationTests")]
[Trait("Category", "Integration")]
public class KidValidationTests : IClassFixture<IntegrationTestBase>, IDisposable
{
    private readonly HttpClient _client;
    private readonly IntegrationTestBase _testBase;

    public KidValidationTests(IntegrationTestBase testBase)
    {
        _testBase = testBase;
        _client = testBase.CreateClient();
    }

    [Fact]
    public async Task UserToken_WithKid_ShouldValidateSuccessfully()
    {
        // Arrange - Generate a user token
        var tokenService = _testBase.Services.GetRequiredService<ITokenService>();
        var tokenResult = tokenService.GenerateTokens(
            userId: "test-user-123",
            email: "test@example.com", 
            role: "user",
            clientId: null,
            tokenType: TokenType.User);

        // Verify token has KID in header
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(tokenResult.AccessToken);
        jwtToken.Header.Kid.Should().NotBeNullOrEmpty();
        jwtToken.Header.Kid.Should().StartWith("test-user-");

        // Act - Use token to access protected endpoint
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenResult.AccessToken);
        var response = await _client.GetAsync("/api/auth/validate");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task M2MToken_WithKid_ShouldValidateSuccessfully()
    {
        // Arrange - Generate an M2M token
        var tokenService = _testBase.Services.GetRequiredService<ITokenService>();
        var tokenResult = tokenService.GenerateTokens(
            userId: "test-client-123",
            email: "client@example.com",
            role: "client", 
            clientId: "test-client",
            tokenType: TokenType.MachineToMachine);

        // Verify token has KID in header
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(tokenResult.AccessToken);
        jwtToken.Header.Kid.Should().NotBeNullOrEmpty();
        jwtToken.Header.Kid.Should().StartWith("test-machinetomachine-");

        // Act - Use token to access protected endpoint
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenResult.AccessToken);
        var response = await _client.GetAsync("/api/auth/validate");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public void BothTokenTypes_ShouldHaveDifferentKids()
    {
        // Arrange
        var tokenService = _testBase.Services.GetRequiredService<ITokenService>();
        
        var userToken = tokenService.GenerateTokens(
            "user-123", "user@test.com", "user", null, TokenType.User);
            
        var m2mToken = tokenService.GenerateTokens(
            "client-123", "client@test.com", "client", "test-client", TokenType.MachineToMachine);

        // Act - Extract KIDs from both tokens
        var handler = new JwtSecurityTokenHandler();
        var userJwt = handler.ReadJwtToken(userToken.AccessToken);
        var m2mJwt = handler.ReadJwtToken(m2mToken.AccessToken);

        // Assert - KIDs should be different and have correct prefixes
        userJwt.Header.Kid.Should().NotBeNullOrEmpty();
        m2mJwt.Header.Kid.Should().NotBeNullOrEmpty();
        userJwt.Header.Kid.Should().NotBe(m2mJwt.Header.Kid);
        
        userJwt.Header.Kid.Should().StartWith("test-user-");
        m2mJwt.Header.Kid.Should().StartWith("test-machinetomachine-");
    }

    [Fact]
    public async Task JwksEndpoint_ShouldContainBothKids()
    {
        // Arrange - Generate tokens to ensure KIDs exist
        var tokenService = _testBase.Services.GetRequiredService<ITokenService>();
        var userToken = tokenService.GenerateTokens("user", "user@test.com", "user", null, TokenType.User);
        var m2mToken = tokenService.GenerateTokens("client", "client@test.com", "client", "test", TokenType.MachineToMachine);

        var handler = new JwtSecurityTokenHandler();
        var userKid = handler.ReadJwtToken(userToken.AccessToken).Header.Kid;
        var m2mKid = handler.ReadJwtToken(m2mToken.AccessToken).Header.Kid;

        // Act - Get JWKS
        var response = await _client.GetAsync("/api/auth/keys/public");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Contain(userKid);
        content.Should().Contain(m2mKid);
        content.Should().Contain("\"keys\":[");
        content.Should().Contain("\"kty\":\"RSA\"");
    }

    public void Dispose()
    {
        _client?.Dispose();
    }
}