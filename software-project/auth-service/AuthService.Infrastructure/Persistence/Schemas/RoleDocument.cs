using HydroEspinaca.Shared.Abstractions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AuthService.Infrastructure.Persistence.Schemas;

public class RoleDocument : IIdentifiableMutable
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!;
    
    [BsonElement("code")]
    public string Code { get; set; } = null!;
    
    [BsonElement("name")]
    public string Name { get; set; } = null!;
    
    [BsonElement("permissions")]
    public List<string> Permissions { get; set; } = new(); // ObjectIds of Permissions

    public void SetId(string id)
    {
        Id = id;
    }
}