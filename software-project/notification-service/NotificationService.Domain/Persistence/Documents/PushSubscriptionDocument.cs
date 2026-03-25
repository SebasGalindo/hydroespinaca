using HydroEspinaca.Shared.Abstractions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace NotificationService.Domain.Persistence.Documents;

/// <summary>
/// MongoDB document representing a push notification subscription, 
/// which stores the details of how to send push notifications to users (e.g., Expo tokens or Web Push subscriptions).
/// </summary>
public class PushSubscriptionDocument : IIdentifiableMutable
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    [BsonElement("user_id")] public string UserId { get; set; } = string.Empty;
    [BsonElement("platform")] public string Platform { get; set; } = string.Empty;
    [BsonElement("token")] public string Token { get; set; } = string.Empty;
    [BsonElement("device_name")] public string? DeviceName { get; set; }
    [BsonElement("is_active")] public bool IsActive { get; set; } = true;
    [BsonElement("last_used_at")] public DateTime LastUsedAt { get; set; }
    [BsonElement("failure_count")] public int FailureCount { get; set; }
    [BsonElement("created_at")] public DateTime CreatedAt { get; set; }

    public void SetId(string id) => Id = id;
}
