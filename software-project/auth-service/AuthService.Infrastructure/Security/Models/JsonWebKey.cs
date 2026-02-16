using System.Text.Json.Serialization;

namespace AuthService.Infrastructure.Security.Models;

/// <summary>
/// Model representing a JSON Web Key (JWK) for public key exposure via JWKS endpoint.
/// </summary>
public class JsonWebKey
{
    [JsonPropertyName("kty")]
    public string KeyType { get; set; } = "RSA";
    
    [JsonPropertyName("kid")]
    public string KeyId { get; set; } = default!;
    
    [JsonPropertyName("use")]
    public string Use { get; set; } = "sig";
    
    [JsonPropertyName("alg")]
    public string Algorithm { get; set; } = "RS256";
    
    [JsonPropertyName("n")]
    public string Modulus { get; set; } = default!;
    
    [JsonPropertyName("e")]
    public string Exponent { get; set; } = default!;
}

/// <summary>
/// Model representing a JSON Web Key Set (JWKS) containing multiple public keys.
/// </summary>
public class JsonWebKeySet
{
    [JsonPropertyName("keys")]
    public List<JsonWebKey> Keys { get; set; } = new();
}