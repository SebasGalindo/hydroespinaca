namespace AuthService.Application.Features.Authentication.DTOs;

/// <summary>
/// DTO for user login request payload.
/// </summary>
public record LoginRequestDto
{
    public string Email { get; init; } = default!;
    public string Password { get; init; } = default!;
}
