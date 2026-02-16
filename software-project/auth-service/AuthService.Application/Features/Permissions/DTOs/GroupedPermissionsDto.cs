
using AuthService.Domain.Entities;

namespace AuthService.Application.Features.Permissions.DTOs;

/// <summary>
/// DTO representing permissions grouped by service or domain area.
/// </summary>
public class GroupedPermissionsDto
{
    public string Category { get; set; } = null!;
    public List<Permission> Permissions { get; set; } = new();
}
