using BffService.Domain.Interfaces;
using BffService.Domain.ValueObjects;
using BffService.Domain.Exceptions;
using BffService.Domain.Constants;
using HydroEspinaca.Shared.DTOs.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Text.Json;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace BffService.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthService> _logger;
    private readonly string _authServiceUrl;
    private readonly string _clientId;
    private readonly string _clientSecret;

    public AuthService(HttpClient httpClient, IConfiguration configuration, ILogger<AuthService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
        _authServiceUrl = _configuration[BffConstants.Auth.AuthServiceUrlConfigKey] 
            ?? throw new InvalidOperationException("AuthServiceUrl not configured");
        _clientId = _configuration[BffConstants.Auth.ClientIdConfigKey] 
            ?? throw new InvalidOperationException("ClientId not configured");
        _clientSecret = _configuration[BffConstants.Auth.ClientSecretConfigKey] 
            ?? throw new InvalidOperationException("ClientSecret not configured");
    }

    public async Task<AuthenticationResult> LoginAsync(
        string email,
        string password,
        string? sessionId = null,
        string? csrfToken = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Attempting authentication with auth service for user: {Email}", email);

            var loginRequest = new
            {
                email,
                password,
                sessionId,
                csrfToken
            };

            var response = await _httpClient.PostAsJsonAsync(
                $"{_authServiceUrl}/api/auth/login",
                loginRequest,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("Authentication failed for user {Email}: {StatusCode} - {Error}",
                    email, response.StatusCode, errorContent);
                throw new InvalidTokenException($"Authentication failed: {response.StatusCode}");
            }

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var tokenResponse = JsonSerializer.Deserialize<TokenResultDto>(responseContent, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            if (tokenResponse == null || string.IsNullOrEmpty(tokenResponse.AccessToken))
            {
                throw new InvalidTokenException("Invalid token response from auth service");
            }

            var claims = ExtractClaimsFromToken(tokenResponse.AccessToken);
            var scopes = tokenResponse.Scopes?.ToList() ?? ExtractScopesFromToken(tokenResponse.AccessToken);

            var expiresAt = tokenResponse.ExpiresAt;
            var refreshTokenExpiresAt = tokenResponse.RefreshTokenExpiresAt ?? DateTime.UtcNow.AddDays(7);

            var tokenInfo = new TokenInfo(
                tokenResponse.AccessToken,
                tokenResponse.RefreshToken ?? string.Empty,
                expiresAt,
                refreshTokenExpiresAt
            );

            var authResult = new AuthenticationResult(
                tokenInfo,
                claims.FirstOrDefault(c => c.Type == "sub")?.Value ?? string.Empty,
                tokenResponse.Username ?? claims.FirstOrDefault(c => c.Type == "name")?.Value ?? string.Empty,
                tokenResponse.Email ?? claims.FirstOrDefault(c => c.Type == "email")?.Value ?? email,
                tokenResponse.Role ?? claims.FirstOrDefault(c => c.Type == "role")?.Value ?? string.Empty,
                scopes
            );

            _logger.LogInformation("Authentication successful for user: {Email}", email);
            return authResult;
        }
        catch (Exception ex) when (ex is not InvalidTokenException)
        {
            _logger.LogError(ex, "Error during authentication for user: {Email}", email);
            throw new InvalidTokenException($"Authentication failed: {ex.Message}");
        }
    }

    public async Task<TokenInfo> RefreshTokenAsync(
        string refreshToken,
        string? sessionId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Attempting token refresh");

            var refreshRequest = new
            {
                refreshToken = refreshToken,
                clientId = _clientId,
                sessionId
            };

            var response = await _httpClient.PostAsJsonAsync(
                $"{_authServiceUrl}/api/auth/refresh",
                refreshRequest,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("Token refresh failed: {StatusCode} - {Error}", response.StatusCode, errorContent);
                throw new InvalidTokenException($"Token refresh failed: {response.StatusCode}");
            }

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var tokenResponse = JsonSerializer.Deserialize<TokenResultDto>(responseContent, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            if (tokenResponse == null || string.IsNullOrEmpty(tokenResponse.AccessToken))
            {
                throw new InvalidTokenException("Invalid token response from auth service");
            }

            var expiresAt = tokenResponse.ExpiresAt;
            var refreshTokenExpiresAt = tokenResponse.RefreshTokenExpiresAt ?? DateTime.UtcNow.AddDays(7);

            _logger.LogInformation("Token refresh successful");

            return new TokenInfo(
                tokenResponse.AccessToken,
                tokenResponse.RefreshToken ?? refreshToken, // Keep old refresh token if new one not provided
                expiresAt,
                refreshTokenExpiresAt
            );
        }
        catch (Exception ex) when (ex is not InvalidTokenException)
        {
            _logger.LogError(ex, "Error during token refresh");
            throw new InvalidTokenException($"Token refresh failed: {ex.Message}");
        }
    }

    public Task<bool> ValidateTokenAsync(string accessToken, CancellationToken cancellationToken = default)
    {
        try
        {
            // Basic JWT validation
            var handler = new JwtSecurityTokenHandler();
            if (!handler.CanReadToken(accessToken))
            {
                return Task.FromResult(false);
            }

            var token = handler.ReadJwtToken(accessToken);

            // Check expiration
            if (token.ValidTo < DateTime.UtcNow)
            {
                return Task.FromResult(false);
            }

            // Check issuer and audience from configuration
            var expectedIssuer = _configuration[BffConstants.Auth.IssuerConfigKey];
            var expectedAudience = _configuration[BffConstants.Auth.AudienceConfigKey];

            var issuer = token.Claims.FirstOrDefault(c => c.Type == "iss")?.Value;
            var audience = token.Claims.FirstOrDefault(c => c.Type == "aud")?.Value;

            if (issuer != expectedIssuer || audience != expectedAudience)
            {
                _logger.LogWarning("Token validation failed: invalid issuer ({Issuer}) or audience ({Audience})", issuer, audience);
                return Task.FromResult(false);
            }

            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating token");
            return Task.FromResult(false);
        }
    }

    public async Task LogoutAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Attempting logout");

            // Note: Auth service may not have a specific logout endpoint, 
            // so we just log the attempt. Token expiration will handle cleanup.
            // In a production system, you might want to add the token to a blacklist
            _logger.LogInformation("Logout successful (client-side token invalidation)");
            
            await Task.CompletedTask; // Satisfy async contract
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout");
            // Don't throw - logout failure shouldn't prevent session cleanup
        }
    }

    private static List<Claim> ExtractClaimsFromToken(string accessToken)
    {
        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(accessToken);
        return token.Claims.ToList();
    }

    private static List<string> ExtractScopesFromToken(string accessToken)
    {
        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(accessToken);
        var scopeClaim = token.Claims.FirstOrDefault(c => c.Type == "scope")?.Value;
        
        if (string.IsNullOrEmpty(scopeClaim))
            return new List<string>();

        return scopeClaim.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();
    }

}