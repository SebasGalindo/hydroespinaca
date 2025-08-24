using Microsoft.IdentityModel.Tokens;

namespace HydroEspinaca.Shared.Authentication.Interfaces;

/// <summary>
/// Interface for resolving JWT signing keys from various sources
/// </summary>
public interface IJwtKeyResolver
{
    /// <summary>
    /// Resolves signing keys for JWT token validation
    /// </summary>
    /// <param name="kid">Key ID from JWT header</param>
    /// <param name="issuer">Token issuer</param>
    /// <returns>Collection of security keys for validation</returns>
    Task<IEnumerable<SecurityKey>> ResolveSigningKeysAsync(string? kid, string issuer);
    
    /// <summary>
    /// Fetches JWKS from the specified endpoint
    /// </summary>
    /// <param name="jwksEndpoint">JWKS endpoint URL</param>
    /// <returns>JSON Web Key Set</returns>
    Task<string> FetchJwksAsync(string jwksEndpoint);
    
    /// <summary>
    /// Converts JWKS JSON to SecurityKey collection
    /// </summary>
    /// <param name="jwksJson">JWKS JSON string</param>
    /// <returns>Collection of security keys</returns>
    IEnumerable<SecurityKey> ConvertJwksToSecurityKeys(string jwksJson);
}

/// <summary>
/// Interface for token validation services
/// </summary>
public interface ITokenValidator
{
    /// <summary>
    /// Validates a JWT token against specified parameters
    /// </summary>
    /// <param name="token">JWT token to validate</param>
    /// <param name="validationParameters">Token validation parameters</param>
    /// <returns>Token validation result</returns>
    Task<SharedTokenValidationResult> ValidateTokenAsync(string token, TokenValidationParameters validationParameters);
    
    /// <summary>
    /// Extracts claims from a validated JWT token
    /// </summary>
    /// <param name="token">JWT token</param>
    /// <returns>Dictionary of claims</returns>
    Dictionary<string, object> ExtractClaims(string token);
    
    /// <summary>
    /// Checks if token has required scope
    /// </summary>
    /// <param name="token">JWT token</param>
    /// <param name="requiredScope">Required scope</param>
    /// <returns>True if token has required scope</returns>
    bool HasRequiredScope(string token, string requiredScope);
}

/// <summary>
/// Shared token validation result
/// </summary>
public class SharedTokenValidationResult
{
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }
    public SecurityToken? ValidatedToken { get; set; }
    public System.Security.Claims.ClaimsPrincipal? Principal { get; set; }
}