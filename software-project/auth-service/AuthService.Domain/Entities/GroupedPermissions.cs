namespace AuthService.Domain.Entities;

public class GroupedPermissions
{
    public string Category { get; set; } = null!;
    public List<Permission> Permissions { get; set; } = new();
}
