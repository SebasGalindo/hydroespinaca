namespace BffService.Application.DTOs;

/// <summary>
/// Session response DTO for frontend consumption with user information
/// </summary>
public record UserSessionDto(
    string UserId,
    string SessionId,
    string Username,
    string Email,
    string Role
);