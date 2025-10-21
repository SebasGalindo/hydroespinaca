namespace HydroEspinaca.Shared.DTOs.Authentication;

/// <summary>
/// Shared token result DTO that represents authentication tokens and metadata
/// This replaces the domain TokenResult class to provide a consistent contract across services
/// </summary>
public record TokenResultDto(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt,
    string? Role = null,
    string? Username = null,
    string? Email = null,
    string? ClientId = null,
    string[]? Scopes = null,
    string? SessionId = null,
    DateTime? RefreshTokenExpiresAt = null
)
{
    public TokenResultDto() : this(string.Empty, string.Empty, DateTime.MinValue, null, null, null, null, null, null, null)
    {
    }
};