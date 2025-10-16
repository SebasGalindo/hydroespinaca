namespace HydroEspinaca.Shared.DTOs.Authentication;

/// <summary>
/// Shared login request DTO used across all authentication services
/// </summary>
public record LoginRequestDto(
    string Email,
    string Password,
    string? SessionId = null,
    string? IpAddress = null,
    string? UserAgent = null,
    string? CsrfToken = null
);