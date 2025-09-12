namespace HydroEspinaca.Shared.DTOs.Authentication;

/// <summary>
/// Request DTO for token refresh through BFF service using session ID
/// </summary>
public record RefreshTokenRequestDto(
    string SessionId
);