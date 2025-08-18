namespace AuthService.Application.Features.Permissions.DTOs;

public record PermissionResponseDto
{
    public string Id { get; init; } = null!;
    public string Code { get; init; } = null!;
    public string Name { get; init; } = null!;
    public string? Description { get; init; }
}