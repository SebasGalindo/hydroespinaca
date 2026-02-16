using HydroEspinaca.Shared.Abstractions;
using MongoDB.Bson;

namespace AuthService.Domain.Entities;

/// <summary>
/// Represents an individual authorization permission that can be assigned to roles.
/// </summary>
public class Permission : IIdentifiableMutable
{
    public string Id { get; private set; } = ObjectId.GenerateNewId().ToString();
    public string Code { get; private set; }
    public string Name { get; private set; }
    public string? Description { get; private set; }

    public Permission(string code, string name, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Permission code cannot be null or empty", nameof(code));
        
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Permission name cannot be null or empty", nameof(name));

      
        Code = code;
        Name = name;
        Description = description;
    }

    public void SetId(string id)
    {
        Id = id;
    }

    public void UpdateDetails(string name, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Permission name cannot be null or empty", nameof(name));
        
        Name = name;
        Description = description;
    }
}