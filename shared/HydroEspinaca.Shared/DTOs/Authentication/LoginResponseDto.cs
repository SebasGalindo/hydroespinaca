namespace HydroEspinaca.Shared.DTOs.Authentication;

/// <summary>
/// Response DTO for successful login through BFF service
/// Contains session information instead of direct JWT tokens
/// </summary>
public record LoginResponseDto(
    string SessionId,
    string CsrfToken,
    string Message = "Login successful"
);