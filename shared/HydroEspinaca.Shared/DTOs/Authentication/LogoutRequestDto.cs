namespace HydroEspinaca.Shared.DTOs.Authentication;

/// <summary>
/// Request DTO for user logout through BFF service
/// </summary>
public record LogoutRequestDto(
    string SessionId
);