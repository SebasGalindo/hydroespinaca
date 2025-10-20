namespace HydroEspinaca.Shared.DTOs.Authentication;

public record UserCreateDto
{
    public string Username { get; init; } = null!;
    public string Email { get; init; } = null!;
    public string Password { get; init; } = null!;
    public string? RoleId { get; init; }
}
