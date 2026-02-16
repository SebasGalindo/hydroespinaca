namespace AuthService.Application.Features.Authentication.DTOs;

/// <summary>
/// DTO for token refresh request payload.
/// </summary>
public record RefreshRequestDto
{
    public string RefreshToken { get; init; } = default!;
    public string? ClientId { get; init; } = default!;
}
