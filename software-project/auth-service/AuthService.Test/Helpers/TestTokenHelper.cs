using AuthService.Domain.Enums;
using AuthService.Domain.Interfaces;
using AuthService.Infrastructure.Security;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.IdentityModel.Tokens.Jwt;

namespace AuthService.Test.Helpers;

/// <summary>
/// Centralized helper for generating test JWT tokens with consistent parameters
/// </summary>
public class TestTokenHelper
{
    private readonly ITokenService _tokenService;
    private readonly JwtSettings _jwtSettings;

    public TestTokenHelper(IServiceProvider serviceProvider)
    {
        _tokenService = serviceProvider.GetRequiredService<ITokenService>();
        _jwtSettings = serviceProvider.GetRequiredService<IOptions<JwtSettings>>().Value;
    }

    /// <summary>
    /// Generates a test token for admin user with admin role
    /// </summary>
    public async Task<string> GenerateAdminTokenAsync()
    {
        Console.WriteLine($"🔧 [TestTokenHelper] Generating admin token with Issuer='{_jwtSettings.Issuer}', Audience='{_jwtSettings.Audience}'");

        var tokens = await _tokenService.GenerateTokensAsync(
            userId: "test-admin-id",
            email: "admin@demo.com",
            role: "admin-role-id",
            clientId: null,
            tokenType: TokenType.User
        );

        var token = tokens.AccessToken;
        LogTokenDetails(token, "ADMIN");
        return token;
    }

    /// <summary>
    /// Generates a test token for regular user
    /// </summary>
    public async Task<string> GenerateUserTokenAsync()
    {
        Console.WriteLine($"🔧 [TestTokenHelper] Generating user token with Issuer='{_jwtSettings.Issuer}', Audience='{_jwtSettings.Audience}'");

        var tokens = await _tokenService.GenerateTokensAsync(
            userId: "test-user-id",
            email: "user@demo.com",
            role: "user-role-id",
            clientId: null,
            tokenType: TokenType.User
        );

        var token = tokens.AccessToken;
        LogTokenDetails(token, "USER");
        return token;
    }

    /// <summary>
    /// Generates a test token with custom parameters
    /// </summary>
    public async Task<string> GenerateCustomTokenAsync(string userId, string email, string role, string? clientId = null, TokenType tokenType = TokenType.User)
    {
        Console.WriteLine($"🔧 [TestTokenHelper] Generating custom token with Issuer='{_jwtSettings.Issuer}', Audience='{_jwtSettings.Audience}'");

        var tokens = await _tokenService.GenerateTokensAsync(
            userId: userId,
            email: email,
            role: role,
            clientId: clientId,
            tokenType: tokenType
        );

        var token = tokens.AccessToken;
        LogTokenDetails(token, "CUSTOM");
        return token;
    }

    /// <summary>
    /// Validates a token and returns whether it's valid
    /// </summary>
    public bool ValidateToken(string token)
    {
        Console.WriteLine($"🔍 [TestTokenHelper] Validating token with expected Issuer='{_jwtSettings.Issuer}', Audience='{_jwtSettings.Audience}'");
        
        var isValid = _tokenService.IsTokenValid(token);
        Console.WriteLine($"🔍 [TestTokenHelper] Token validation result: {isValid}");
        return isValid;
    }

    /// <summary>
    /// Logs detailed information about a token for debugging
    /// </summary>
    private void LogTokenDetails(string token, string tokenType)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jsonToken = handler.ReadJwtToken(token);

            Console.WriteLine($"🎫 [TestTokenHelper] {tokenType} Token Details:");
            Console.WriteLine($"   - Algorithm: {jsonToken.Header.Alg}");
            Console.WriteLine($"   - Issuer: {jsonToken.Issuer}");
            Console.WriteLine($"   - Audience: {string.Join(", ", jsonToken.Audiences)}");
            Console.WriteLine($"   - Subject: {jsonToken.Subject}");
            Console.WriteLine($"   - Expires: {jsonToken.ValidTo:yyyy-MM-dd HH:mm:ss} UTC");
            Console.WriteLine($"   - Not Before: {jsonToken.ValidFrom:yyyy-MM-dd HH:mm:ss} UTC");
            Console.WriteLine($"   - Token Length: {token.Length}");
            
            // Log claims
            Console.WriteLine($"   - Claims:");
            foreach (var claim in jsonToken.Claims)
            {
                Console.WriteLine($"     * {claim.Type}: {claim.Value}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ [TestTokenHelper] Error reading token details: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets current JWT settings for debugging
    /// </summary>
    public void LogJwtSettings()
    {
        Console.WriteLine($"⚙️ [TestTokenHelper] Current JWT Settings:");
        Console.WriteLine($"   - Issuer: '{_jwtSettings.Issuer}'");
        Console.WriteLine($"   - Audience: '{_jwtSettings.Audience}'");
        Console.WriteLine($"   - AccessTokenExpiryMinutes: {_jwtSettings.AccessTokenExpiryMinutes}");
    }
}