using HydroEspinaca.Shared.Abstractions;
using HydroEspinaca.Shared.Enums;
using MongoDB.Bson.Serialization.Attributes;

namespace AuthService.Infrastructure.Persistence.Schemas;
public class UserDocument : IIdentifiableMutable
{
    public string Id { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string Password { get; set; } = null!;
    public Role Role { get; set; }

    public void SetId(string id)
    {
        Id = id;
    }
}