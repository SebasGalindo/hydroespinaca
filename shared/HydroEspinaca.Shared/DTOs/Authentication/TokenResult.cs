namespace HydroEspinaca.Shared.DTOs.Authentication;

/// <summary>
/// Result of a token generation operation
/// </summary>
public class TokenResult
{
    /// <summary>
    /// JWT access token
    /// </summary>
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>
    /// Refresh token for obtaining new access tokens
    /// </summary>
    public string RefreshToken { get; set; } = string.Empty;

    /// <summary>
    /// When the access token expires
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// User role (for user tokens)
    /// </summary>
    public string? Role { get; set; }

    /// <summary>
    /// Client ID (for M2M tokens)
    /// </summary>
    public string? ClientId { get; set; }

    /// <summary>
    /// Scopes granted in this token
    /// </summary>
    public string[] Scopes { get; set; } = Array.Empty<string>();
}