using HydroEspinaca.Shared.Abstractions;
using MongoDB.Bson;

namespace AuthService.Domain.Entities;

/// <summary>
/// Represents an authorization role that groups a set of permissions and can be assigned to users.
/// </summary>
public class Role : IIdentifiableMutable
{
    public string Id { get; private set; } = ObjectId.GenerateNewId().ToString();
    public string Code { get; private set; }
    public string Name { get; private set; }
    public List<string> Permissions { get; private set; }

    public Role(string code, string name, List<string>? permissions = null)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Role code cannot be null or empty", nameof(code));
        
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Role name cannot be null or empty", nameof(name));

        if (!code.StartsWith("role_"))
            throw new ArgumentException("Role code must start with 'role_'", nameof(code));

        Code = code;
        Name = name;
        Permissions = permissions ?? new List<string>();
    }

    public void SetId(string id)
    {
        Id = id;
    }

    public void UpdateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Role name cannot be null or empty", nameof(name));
        
        Name = name;
    }

    public void SetPermissions(List<string> permissions)
    {
        Permissions = permissions ?? new List<string>();
    }
}