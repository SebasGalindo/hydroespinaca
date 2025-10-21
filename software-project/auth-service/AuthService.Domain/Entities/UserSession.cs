using HydroEspinaca.Shared.Abstractions;

namespace AuthService.Domain.Entities;

public class UserSession : IIdentifiableMutable
{
    public string Id { get; private set; }
    public string UserId { get; private set; }
    public string ClientId { get; private set; }
    public string SessionId { get; private set; }
    public string RefreshToken { get; private set; }
    public string AccessTokenHash { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public DateTime LastActivity { get; private set; }
    public bool Revoked { get; private set; }
    public DateTime? RevokedAt { get; private set; }
    public string? IpAddress { get; private set; }
    public string? UserAgent { get; private set; }
    public string? CsrfToken { get; private set; }

    public UserSession(
        string userId,
        string clientId,
        string sessionId,
        string refreshToken,
        string accessTokenHash,
        DateTime expiresAt,
        string? ipAddress = null,
        string? userAgent = null,
        string? csrfToken = null)
    {
        Id = Guid.NewGuid().ToString();
        UserId = userId;
        ClientId = clientId;
        SessionId = sessionId;
        RefreshToken = refreshToken;
        AccessTokenHash = accessTokenHash;
        CreatedAt = DateTime.UtcNow;
        ExpiresAt = expiresAt;
        LastActivity = DateTime.UtcNow;
        Revoked = false;
        IpAddress = ipAddress;
        UserAgent = userAgent;
        CsrfToken = csrfToken;
    }

    public void SetId(string id) => Id = id;

    public void RestoreTimestamps(DateTime createdAt, DateTime lastActivity)
    {
        CreatedAt = createdAt;
        LastActivity = lastActivity;
    }

    public void UpdateTokens(string refreshToken, string accessTokenHash)
    {
        RefreshToken = refreshToken;
        AccessTokenHash = accessTokenHash;
        LastActivity = DateTime.UtcNow;
        // ExpiresAt is NOT updated - it remains fixed from the initial login
    }

    public void UpdateActivity()
    {
        LastActivity = DateTime.UtcNow;
    }

    public void Revoke()
    {
        Revoked = true;
        RevokedAt = DateTime.UtcNow;
    }

    public bool IsActive => !Revoked && DateTime.UtcNow < ExpiresAt;
}
