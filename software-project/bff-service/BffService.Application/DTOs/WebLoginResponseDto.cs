namespace BffService.Application.DTOs;

/// <summary>
/// Response DTO for web login endpoint
/// Returns session info for JavaScript access while also setting cookies
/// </summary>
public record WebLoginResponseDto(
    string Message = "Login successful",
    string? SessionId = null,
    string? CsrfToken = null
);