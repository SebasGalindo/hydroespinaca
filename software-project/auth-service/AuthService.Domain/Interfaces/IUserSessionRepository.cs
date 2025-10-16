using AuthService.Domain.Entities;

namespace AuthService.Domain.Interfaces;

public interface IUserSessionRepository
{
    Task<UserSession?> FindBySessionIdAsync(string sessionId);
    Task<UserSession?> FindByRefreshTokenAsync(string refreshToken);
    Task<IEnumerable<UserSession>> FindActiveByUserIdAsync(string userId);
    Task<IEnumerable<UserSession>> FindAllByUserIdAsync(string userId);
    Task AddAsync(UserSession session);
    Task UpdateAsync(UserSession session);
    Task DeleteAsync(string id);
    Task RevokeAllByUserIdAsync(string userId);
}
