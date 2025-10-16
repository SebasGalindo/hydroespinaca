namespace BffService.Application.DTOs;

/// <summary>
/// Session response DTO for frontend consumption with user information
/// </summary>
public record UserSessionDto(
    string Username,
    string Email,
    string Role
);