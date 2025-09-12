using System.Text.Json.Serialization;

namespace HydroEspinaca.Shared.DTOs.Authentication;

/// <summary>
/// JSON Web Key Set (JWKS) DTO for public key distribution
/// </summary>
public class JsonWebKeySetDto
{
    [JsonPropertyName("keys")]
    public List<JsonWebKeyDto> Keys { get; set; } = new();
}

/// <summary>
/// JSON Web Key DTO representing a single public key
/// </summary>
public class JsonWebKeyDto
{
    [JsonPropertyName("kty")]
    public string KeyType { get; set; } = string.Empty;

    [JsonPropertyName("use")]
    public string Use { get; set; } = string.Empty;

    [JsonPropertyName("kid")]
    public string KeyId { get; set; } = string.Empty;

    [JsonPropertyName("alg")]
    public string Algorithm { get; set; } = string.Empty;

    [JsonPropertyName("n")]
    public string Modulus { get; set; } = string.Empty;

    [JsonPropertyName("e")]
    public string Exponent { get; set; } = string.Empty;
}

/// <summary>
/// Token validation request DTO
/// </summary>
public class TokenValidationRequestDto
{
    public string Token { get; set; } = string.Empty;
    public string? RequiredScope { get; set; }
    public string? RequiredAudience { get; set; }
    public string? RequiredIssuer { get; set; }
}

/// <summary>
/// Token validation result DTO
/// </summary>
public class TokenValidationResultDto
{
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }
    public Dictionary<string, object> Claims { get; set; } = new();
    public DateTime? ExpiresAt { get; set; }
    public string? Subject { get; set; }
    public List<string> Scopes { get; set; } = new();
}