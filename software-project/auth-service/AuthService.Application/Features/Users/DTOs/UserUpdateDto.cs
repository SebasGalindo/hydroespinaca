namespace AuthService.Application.Features.Users.DTOs;

public record UserUpdateDto
{
    public string? Username { get; init; }
    public string? Email { get; init; }
    public string? Password { get; init; }
    public string? RoleId { get; init; }
}