namespace AuthService.Application.Features.Roles.DTOs;

public record UpdateRoleRequestDto
{
    public string Name { get; init; } = null!;
    public List<string> PermissionCodes { get; init; } = new();
}