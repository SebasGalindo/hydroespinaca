namespace AuthService.Application.Features.Authentication.DTOs;

public record RefreshRequestDto
{
    public string RefreshToken { get; init; } = default!;
    public string? ClientId { get; init; } = default!;
}
