using HydroEspinaca.Shared.Abstractions;
using HydroEspinaca.Shared.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AuthService.Infrastructure.Persistence.Schemas;
/// <summary>
/// MongoDB document schema for user records.
/// </summary>
public class UserDocument : IIdentifiableMutable
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!;

    public string Username { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string Password { get; set; } = null!;

    [BsonRepresentation(BsonType.ObjectId)]
    public string? RoleId { get; set; }

    public void SetId(string id)
    {
        Id = id;
    }
}