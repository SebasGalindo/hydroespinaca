using HydroEspinaca.Shared.Abstractions;
using MongoDB.Bson;

namespace AuthService.Domain.Entities;

public class Role : IIdentifiableMutable
{
    public string Id { get; private set; } = ObjectId.GenerateNewId().ToString();
    public string Code { get; private set; }
    public string Name { get; private set; }
    public List<string> Permissions { get; private set; } // ObjectIds of Permissions

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

    public void UpdateCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Role code cannot be null or empty", nameof(code));
        
        if (!code.StartsWith("role_"))
            throw new ArgumentException("Role code must start with 'role_'", nameof(code));
        
        Code = code;
    }

    public void SetPermissions(List<string> permissions)
    {
        Permissions = permissions ?? new List<string>();
    }

    public void AddPermission(string permissionId)
    {
        if (string.IsNullOrWhiteSpace(permissionId))
            throw new ArgumentException("Permission ID cannot be null or empty", nameof(permissionId));
        
        if (!Permissions.Contains(permissionId))
            Permissions.Add(permissionId);
    }

    public void RemovePermission(string permissionId)
    {
        Permissions.Remove(permissionId);
    }

    public bool HasPermission(string permissionId)
    {
        return Permissions.Contains(permissionId);
    }
}