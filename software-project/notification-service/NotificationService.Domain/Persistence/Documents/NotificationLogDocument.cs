using HydroEspinaca.Shared.Abstractions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace NotificationService.Domain.Persistence.Documents;

/// <summary>
/// MongoDB document representing a notification log entry, which tracks all sent notifications across all channels.
/// This document is stored in the "notification_log" collection and serves as a unified log for
/// all notifications, replacing the previous email-only log to support the multi-channel notification pipeline.
/// </summary>
public class NotificationLogDocument : IIdentifiableMutable
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    [BsonElement("correlation_id")] public string CorrelationId { get; set; } = string.Empty;
    [BsonElement("user_id")] public string UserId { get; set; } = string.Empty;
    [BsonElement("channel")] public string Channel { get; set; } = string.Empty;
    [BsonElement("template_key")] public string TemplateKey { get; set; } = string.Empty;
    [BsonElement("title")] public string Title { get; set; } = string.Empty;
    [BsonElement("status")] public string Status { get; set; } = "queued";
    [BsonElement("provider")] public string? Provider { get; set; }
    [BsonElement("provider_message_id")] public string? ProviderMessageId { get; set; }
    [BsonElement("error")] public string? Error { get; set; }
    [BsonElement("created_at")] public DateTime CreatedAt { get; set; }
    [BsonElement("updated_at")] public DateTime? UpdatedAt { get; set; }

    public void SetId(string id) => Id = id;
}
