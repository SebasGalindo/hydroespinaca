namespace HydroEspinaca.Shared.DTOs.Authentication;

/// <summary>
/// Shared token refresh request DTO used across all authentication services
/// </summary>
public record RefreshRequestDto(
    string RefreshToken,
    string? ClientId = null,
    string? SessionId = null,
    string? IpAddress = null,
    string? UserAgent = null
);