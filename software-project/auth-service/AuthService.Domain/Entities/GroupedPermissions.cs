namespace AuthService.Domain.Entities;

/// <summary>
/// Represents a collection of permissions grouped by their service or domain area.
/// </summary>
public class GroupedPermissions
{
    public string Category { get; set; } = null!;
    public List<Permission> Permissions { get; set; } = new();
}
