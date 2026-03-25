using AuthService.Domain.Entities;

namespace AuthService.Domain.Interfaces;

/// <summary>
/// Persistence contract for user session entities.
/// </summary>
public interface IUserSessionRepository
{
    Task<UserSession?> FindBySessionIdAsync(string sessionId);
    Task<UserSession?> FindByRefreshTokenAsync(string refreshToken);
    Task<IEnumerable<UserSession>> FindActiveByUserIdAsync(string userId);
    Task<Dictionary<string, List<UserSession>>> GetAllActiveSessionsGroupedByUserAsync();
    Task AddAsync(UserSession session);
    Task UpdateAsync(UserSession session);
}
