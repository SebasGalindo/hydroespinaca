namespace BffService.Application.DTOs;

/// <summary>
/// Minimal session response DTO for frontend consumption - only essential user information
/// </summary>
public record UserSessionDto(
    string UserId,
    string Role
);