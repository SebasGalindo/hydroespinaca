namespace AuthService.Application.DTOs;

public record UpdateRoleRequestDto
{
    public string Name { get; init; } = null!;
    public List<string> PermissionCodes { get; init; } = new(); // Accept permission codes from API
}