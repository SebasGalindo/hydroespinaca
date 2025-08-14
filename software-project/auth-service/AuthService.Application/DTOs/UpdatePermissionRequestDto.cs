namespace AuthService.Application.DTOs;

public record UpdatePermissionRequestDto
{
    public string Name { get; init; } = null!;
    public string? Description { get; init; }
}