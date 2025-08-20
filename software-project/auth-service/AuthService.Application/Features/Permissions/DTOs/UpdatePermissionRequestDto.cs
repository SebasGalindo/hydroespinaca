namespace AuthService.Application.Features.Permissions.DTOs;

public record UpdatePermissionRequestDto
{
    public string Name { get; init; } = null!;
    public string? Description { get; init; }
}