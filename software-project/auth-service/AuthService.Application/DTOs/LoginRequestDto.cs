namespace AuthService.Application.DTOs;

public record LoginRequestDto
{
    public string Email { get; init; } = default!;
    public string Password { get; init; } = default!;
}
