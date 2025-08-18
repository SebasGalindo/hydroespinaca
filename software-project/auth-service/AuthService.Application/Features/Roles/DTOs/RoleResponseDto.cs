namespace AuthService.Application.Features.Roles.DTOs;

public record RoleResponseDto
{
    public string Id { get; init; } = null!;
    public string Code { get; init; } = null!;
    public string Name { get; init; } = null!;
    public List<string> PermissionCodes { get; init; } = new();
}