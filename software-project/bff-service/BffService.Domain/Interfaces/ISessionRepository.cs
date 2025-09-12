using BffService.Domain.Entities;

namespace BffService.Domain.Interfaces;

public interface ISessionRepository
{
    Task<Session?> GetAsync(string sessionId, CancellationToken cancellationToken = default);
    Task SaveAsync(Session session, CancellationToken cancellationToken = default);
    Task DeleteAsync(string sessionId, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string sessionId, CancellationToken cancellationToken = default);
    Task CleanupExpiredSessionsAsync(CancellationToken cancellationToken = default);
}