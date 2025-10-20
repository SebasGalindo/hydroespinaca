using AuthService.Domain.Entities;

namespace AuthService.Domain.Interfaces;

public interface IUserSessionService
{
    Task<UserSession> CreateSessionAsync(
        string userId,
        string clientId,
        string sessionId,
        string refreshToken,
        string accessToken,
        DateTime expiresAt,
        string? ipAddress = null,
        string? userAgent = null,
        string? csrfToken = null);

    Task<UserSession> RefreshSessionAsync(
        string refreshToken,
        string newRefreshToken,
        string newAccessToken,
        DateTime newExpiresAt);

    Task<IEnumerable<UserSession>> GetActiveUserSessionsAsync(string userId);
}
