using HydroEspinaca.Shared.Authentication.Interfaces;
using HydroEspinaca.Shared.DTOs.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;
using System.Text.Json;

namespace HydroEspinaca.Shared.Authentication.Services;

/// <summary>
/// Service for resolving JWT signing keys from JWKS endpoints
/// </summary>
public class JwtKeyResolver : IJwtKeyResolver
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<JwtKeyResolver> _logger;
    private readonly Dictionary<string, (JsonWebKeySetDto Keys, DateTime CachedAt)> _keyCache = new();
    private readonly TimeSpan _cacheExpiry = TimeSpan.FromMinutes(15);

    public JwtKeyResolver(HttpClient httpClient, ILogger<JwtKeyResolver> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<IEnumerable<SecurityKey>> ResolveSigningKeysAsync(string? kid, string issuer)
    {
        try
        {
            // Construct JWKS endpoint URL
            var jwksEndpoint = $"{issuer.TrimEnd('/')}/api/auth/keys/public";
            Console.WriteLine($"JWKS Endpoint: {jwksEndpoint}");
            
            // Try to get from cache first
            if (_keyCache.TryGetValue(jwksEndpoint, out var cached) &&
                DateTime.UtcNow - cached.CachedAt < _cacheExpiry)
            {
                _logger.LogDebug("Using cached JWKS for {Issuer}", issuer);
                return ConvertJwksToSecurityKeys(cached.Keys, kid);
            }

            // Fetch fresh JWKS
            var jwksJson = await FetchJwksAsync(jwksEndpoint);
            var jwks = JsonSerializer.Deserialize<JsonWebKeySetDto>(jwksJson);
            
            if (jwks == null)
            {
                _logger.LogWarning("Failed to deserialize JWKS from {Endpoint}", jwksEndpoint);
                return Enumerable.Empty<SecurityKey>();
            }

            // Cache the result
            _keyCache[jwksEndpoint] = (jwks, DateTime.UtcNow);

            return ConvertJwksToSecurityKeys(jwks, kid);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to resolve signing keys for issuer {Issuer}", issuer);
            return Enumerable.Empty<SecurityKey>();
        }
    }

    public async Task<string> FetchJwksAsync(string jwksEndpoint)
    {
        try
        {
            _logger.LogDebug("Fetching JWKS from {Endpoint}", jwksEndpoint);
            
            var response = await _httpClient.GetAsync(jwksEndpoint);
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync();
            _logger.LogDebug("Successfully fetched JWKS from {Endpoint}", jwksEndpoint);
            
            return content;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error fetching JWKS from {Endpoint}", jwksEndpoint);
            throw new InvalidOperationException($"Failed to fetch JWKS from {jwksEndpoint}: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error fetching JWKS from {Endpoint}", jwksEndpoint);
            throw;
        }
    }

    public IEnumerable<SecurityKey> ConvertJwksToSecurityKeys(string jwksJson)
    {
        try
        {
            var jwks = JsonSerializer.Deserialize<JsonWebKeySetDto>(jwksJson);
            if (jwks == null)
            {
                return Enumerable.Empty<SecurityKey>();
            }

            return ConvertJwksToSecurityKeys(jwks, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to convert JWKS JSON to SecurityKeys");
            return Enumerable.Empty<SecurityKey>();
        }
    }

    private IEnumerable<SecurityKey> ConvertJwksToSecurityKeys(JsonWebKeySetDto jwks, string? kid)
    {
        var keys = new List<SecurityKey>();

        foreach (var key in jwks.Keys)
        {
            try
            {
                // If kid is specified, only return matching key
                if (!string.IsNullOrEmpty(kid) && key.KeyId != kid)
                    continue;

                if (key.KeyType == "RSA")
                {
                    var securityKey = CreateRsaSecurityKey(key);
                    if (securityKey != null)
                    {
                        keys.Add(securityKey);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to convert JWK with kid {Kid} to SecurityKey", key.KeyId);
            }
        }

        return keys;
    }

    private RsaSecurityKey? CreateRsaSecurityKey(JsonWebKeyDto jwk)
    {
        try
        {
            var rsa = RSA.Create();
            
            // Convert base64url to bytes
            var modulus = Base64UrlEncoder.DecodeBytes(jwk.Modulus);
            var exponent = Base64UrlEncoder.DecodeBytes(jwk.Exponent);
            
            // Set RSA parameters
            var parameters = new RSAParameters
            {
                Modulus = modulus,
                Exponent = exponent
            };
            
            rsa.ImportParameters(parameters);
            
            return new RsaSecurityKey(rsa)
            {
                KeyId = jwk.KeyId
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to create RSA security key from JWK with kid {Kid}", jwk.KeyId);
            return null;
        }
    }
}