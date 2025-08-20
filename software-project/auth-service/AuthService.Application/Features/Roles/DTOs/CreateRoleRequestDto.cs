namespace AuthService.Application.Features.Roles.DTOs;

public record CreateRoleRequestDto
{
    public string Code { get; init; } = null!;
    public string Name { get; init; } = null!;
    public List<string> PermissionCodes { get; init; } = new();
}