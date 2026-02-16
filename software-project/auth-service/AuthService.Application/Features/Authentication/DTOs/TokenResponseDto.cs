namespace AuthService.Application.Features.Authentication.DTOs;

/// <summary>
/// DTO containing the authentication response with access token, refresh token, and expiration metadata.
/// </summary>
public record TokenResponseDto
{
    public string AccessToken { get; init; } = default!;
    public string RefreshToken { get; init; } = default!;
    public DateTime ExpiresAt { get; init; }
    public string Role { get; init; } = default!;
    public string? ClientId { get; init; }
}
