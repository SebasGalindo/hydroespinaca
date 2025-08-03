using HydroEspinaca.Shared.Abstractions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AuthService.Infrastructure.Persistence.Schemas;

public class ClientAppDocument : IIdentifiableMutable
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!;

    [BsonElement("clientId")]
    public string ClientId { get; set; } = null!;

    [BsonElement("secretHash")]
    public string SecretHash { get; set; } = null!;

    [BsonElement("scopes")]
    public List<string> Scopes { get; set; } = new();

    public void SetId(string id) => Id = id;
}