using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using HydroEspinaca.Shared.Abstractions;

namespace AuthService.Infrastructure.Persistence.Schemas;

/// <summary>
/// MongoDB document for password reset tokens
/// </summary>
public class PasswordResetTokenDocument : IIdentifiableMutable
{
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public string Id { get; set; } = string.Empty;
    
    [BsonElement("userId")]
    [BsonRepresentation(BsonType.String)]
    public string UserId { get; set; } = string.Empty;
    
    [BsonElement("code")]
    public string Code { get; set; } = string.Empty;
    
    [BsonElement("expiresAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime ExpiresAt { get; set; }
    
    [BsonElement("isUsed")]
    public bool IsUsed { get; set; }
    
    [BsonElement("createdAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Sets the ID of the password reset token document (required by IIdentifiableMutable)
    /// </summary>
    /// <param name="id">The ID to set</param>
    public void SetId(string id)
    {
        Id = id;
    }
}