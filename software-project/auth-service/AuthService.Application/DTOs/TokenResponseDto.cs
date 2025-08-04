namespace AuthService.Application.DTOs;

public record TokenResponseDto
{
    public string AccessToken { get; init; } = default!;
    public string RefreshToken { get; init; } = default!;
    public DateTime ExpiresAt { get; init; }
    public string Role { get; init; } = default!;
    public string? ClientId { get; init; }
}
