namespace AuthService.Application.Features.Users.DTOs;

public record UserResponseDto
{
    public string Id { get; init; } = null!;
    public string Username { get; init; } = null!;
    public string Email { get; init; } = null!;
    public string? RoleId { get; init; }
}