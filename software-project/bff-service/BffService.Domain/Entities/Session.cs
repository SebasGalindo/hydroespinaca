namespace BffService.Domain.Entities;

public class Session
{
    public string SessionId { get; private set; }
    public string CsrfToken { get; private set; }
    public string AccessToken { get; private set; }
    public string RefreshToken { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public DateTime? RefreshTokenExpiresAt { get; private set; }
    public string? UserId { get; private set; }
    public string? UserRole { get; private set; }
    public List<string> Scopes { get; private set; }

    private Session(string sessionId, string csrfToken)
    {
        SessionId = sessionId;
        CsrfToken = csrfToken;
        CreatedAt = DateTime.UtcNow;
        Scopes = new List<string>();
    }

    public static Session Create(string sessionId, string csrfToken)
    {
        return new Session(sessionId, csrfToken);
    }

    public void SetTokens(string accessToken, string refreshToken, DateTime expiresAt, DateTime? refreshTokenExpiresAt = null)
    {
        AccessToken = accessToken;
        RefreshToken = refreshToken;
        ExpiresAt = expiresAt;
        RefreshTokenExpiresAt = refreshTokenExpiresAt;
    }

    public void SetUserInfo(string userId, string userRole, List<string> scopes)
    {
        UserId = userId;
        UserRole = userRole;
        Scopes = scopes ?? new List<string>();
    }

    public void UpdateAccessToken(string accessToken, DateTime expiresAt)
    {
        AccessToken = accessToken;
        ExpiresAt = expiresAt;
    }

    public bool IsExpired()
    {
        return DateTime.UtcNow >= ExpiresAt;
    }

    public bool IsRefreshTokenExpired()
    {
        return RefreshTokenExpiresAt.HasValue && DateTime.UtcNow >= RefreshTokenExpiresAt.Value;
    }

    public bool HasValidTokens()
    {
        return !string.IsNullOrEmpty(AccessToken) && !IsExpired();
    }

    public bool CanRefresh()
    {
        return !string.IsNullOrEmpty(RefreshToken) && !IsRefreshTokenExpired();
    }

    public void Invalidate()
    {
        AccessToken = string.Empty;
        RefreshToken = string.Empty;
        ExpiresAt = DateTime.UtcNow.AddSeconds(-1);
    }
}