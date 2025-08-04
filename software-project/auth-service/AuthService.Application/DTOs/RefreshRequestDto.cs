namespace AuthService.Application.DTOs;

public record RefreshRequestDto
{
    public string RefreshToken { get; init; } = default!;
    public string? ClientId { get; init; } = default!;
}
