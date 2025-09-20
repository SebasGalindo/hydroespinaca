namespace BffService.Application.DTOs;

/// <summary>
/// Response DTO for mobile login endpoint
/// Returns session data in the response body for mobile secure storage
/// </summary>
public record MobileLoginResponseDto(
    string SessionId,
    string CsrfToken,
    string Message = "Login successful"
);