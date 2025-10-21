using MongoDB.Bson.Serialization.Attributes;
using HydroEspinaca.Shared.Abstractions;

namespace AuthService.Infrastructure.Persistence.Schemas;

public class UserSessionDocument : IIdentifiableMutable
{
    [BsonId]
    [BsonRepresentation(MongoDB.Bson.BsonType.String)]
    public string Id { get; set; } = default!;

    [BsonElement("userId")]
    public string UserId { get; set; } = default!;

    [BsonElement("clientId")]
    public string ClientId { get; set; } = default!;

    [BsonElement("sessionId")]
    public string SessionId { get; set; } = default!;

    [BsonElement("refreshToken")]
    public string RefreshToken { get; set; } = default!;

    [BsonElement("accessTokenHash")]
    public string AccessTokenHash { get; set; } = default!;

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; }

    [BsonElement("expiresAt")]
    public DateTime ExpiresAt { get; set; }

    [BsonElement("lastActivity")]
    public DateTime LastActivity { get; set; }

    [BsonElement("revoked")]
    public bool Revoked { get; set; }

    [BsonElement("revokedAt")]
    public DateTime? RevokedAt { get; set; }

    [BsonElement("ipAddress")]
    public string? IpAddress { get; set; }

    [BsonElement("userAgent")]
    public string? UserAgent { get; set; }

    [BsonElement("csrfToken")]
    public string? CsrfToken { get; set; }

    public void SetId(string id) => Id = id;
}
