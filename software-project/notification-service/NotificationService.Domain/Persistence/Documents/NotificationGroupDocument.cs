using HydroEspinaca.Shared.Abstractions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace NotificationService.Domain.Persistence.Documents;

public class NotificationGroupDocument : IIdentifiableMutable
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    [BsonElement("groupName")]
    public string GroupName { get; set; } = string.Empty;

    [BsonElement("description")]
    public string? Description { get; set; }

    [BsonElement("recipients")]
    public List<GroupRecipientDocument> Recipients { get; set; } = [];

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; }

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; }

    public void SetId(string id) { Id = id; }
}

public class GroupRecipientDocument
{
    [BsonElement("email")]
    public string Email { get; set; } = string.Empty;

    [BsonElement("type")]
    public string Type { get; set; } = "TO"; // Stored as string: "TO", "CC", "BCC"

    [BsonElement("isActive")]
    public bool IsActive { get; set; } = true;
}