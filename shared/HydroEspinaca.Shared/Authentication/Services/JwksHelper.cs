using HydroEspinaca.Shared.Authentication.Interfaces;
using HydroEspinaca.Shared.DTOs.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace HydroEspinaca.Shared.Authentication.Services;

/// <summary>
/// Helper service for JWT token validation and scope checking
/// </summary>
public class JwksHelper : ITokenValidator
{
    private readonly IJwtKeyResolver _keyResolver;
    private readonly ILogger<JwksHelper> _logger;
    private readonly JwtSecurityTokenHandler _tokenHandler;

    public JwksHelper(IJwtKeyResolver keyResolver, ILogger<JwksHelper> logger)
    {
        _keyResolver = keyResolver;
        _logger = logger;
        _tokenHandler = new JwtSecurityTokenHandler();
    }

    public async Task<SharedTokenValidationResult> ValidateTokenAsync(string token, TokenValidationParameters validationParameters)
    {
        try
        {
            // Parse token to get issuer and kid
            var jwtToken = _tokenHandler.ReadJwtToken(token);
            var issuer = jwtToken.Issuer;
            var kid = jwtToken.Header.Kid;

            // Resolve signing keys
            var signingKeys = await _keyResolver.ResolveSigningKeysAsync(kid, issuer);
            
            if (!signingKeys.Any())
            {
                return new SharedTokenValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "No signing keys found for token validation"
                };
            }

            // Update validation parameters with resolved keys
            validationParameters.IssuerSigningKeys = signingKeys;

            // Validate token
            var principal = _tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);

            return new SharedTokenValidationResult
            {
                IsValid = true,
                ValidatedToken = validatedToken,
                Principal = principal
            };
        }
        catch (SecurityTokenException ex)
        {
            _logger.LogWarning(ex, "Token validation failed: {Message}", ex.Message);
            return new SharedTokenValidationResult
            {
                IsValid = false,
                ErrorMessage = ex.Message
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during token validation");
            return new SharedTokenValidationResult
            {
                IsValid = false,
                ErrorMessage = "Token validation failed due to an unexpected error"
            };
        }
    }

    public Dictionary<string, object> ExtractClaims(string token)
    {
        try
        {
            var jwtToken = _tokenHandler.ReadJwtToken(token);
            
            var claims = new Dictionary<string, object>();
            
            foreach (var claim in jwtToken.Claims)
            {
                if (claims.ContainsKey(claim.Type))
                {
                    // Handle multiple claims of the same type
                    if (claims[claim.Type] is List<string> existingList)
                    {
                        existingList.Add(claim.Value);
                    }
                    else
                    {
                        claims[claim.Type] = new List<string> { claims[claim.Type].ToString()!, claim.Value };
                    }
                }
                else
                {
                    claims[claim.Type] = claim.Value;
                }
            }

            return claims;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to extract claims from token");
            return new Dictionary<string, object>();
        }
    }

    public bool HasRequiredScope(string token, string requiredScope)
    {
        try
        {
            var jwtToken = _tokenHandler.ReadJwtToken(token);
            
            // Get all scope claims
            var scopeClaims = jwtToken.Claims.Where(c => c.Type == "scope").ToList();
            
            foreach (var scopeClaim in scopeClaims)
            {
                if (string.IsNullOrEmpty(scopeClaim.Value))
                    continue;

                // Handle space-separated scopes (OAuth 2.0 standard)
                var scopes = scopeClaim.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                
                if (scopes.Contains(requiredScope))
                {
                    return true;
                }
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check required scope in token");
            return false;
        }
    }

    /// <summary>
    /// Validates a token and checks if it has the required scope
    /// </summary>
    public async Task<TokenValidationResultDto> ValidateTokenWithScopeAsync(
        TokenValidationRequestDto request, 
        TokenValidationParameters validationParameters)
    {
        try
        {
            // Validate token
            var validationResult = await ValidateTokenAsync(request.Token, validationParameters);
            
            if (!validationResult.IsValid)
            {
                return new TokenValidationResultDto
                {
                    IsValid = false,
                    ErrorMessage = validationResult.ErrorMessage
                };
            }

            // Extract claims
            var claims = ExtractClaims(request.Token);
            var jwtToken = _tokenHandler.ReadJwtToken(request.Token);
            
            // Check required scope if specified
            if (!string.IsNullOrEmpty(request.RequiredScope))
            {
                if (!HasRequiredScope(request.Token, request.RequiredScope))
                {
                    return new TokenValidationResultDto
                    {
                        IsValid = false,
                        ErrorMessage = $"Token does not have required scope: {request.RequiredScope}"
                    };
                }
            }

            // Extract scopes
            var scopes = new List<string>();
            var scopeClaims = jwtToken.Claims.Where(c => c.Type == "scope");
            foreach (var scopeClaim in scopeClaims)
            {
                if (!string.IsNullOrEmpty(scopeClaim.Value))
                {
                    scopes.AddRange(scopeClaim.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries));
                }
            }

            return new TokenValidationResultDto
            {
                IsValid = true,
                Claims = claims,
                ExpiresAt = jwtToken.ValidTo,
                Subject = jwtToken.Subject,
                Scopes = scopes.Distinct().ToList()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate token with scope check");
            return new TokenValidationResultDto
            {
                IsValid = false,
                ErrorMessage = "Token validation failed due to an unexpected error"
            };
        }
    }
}