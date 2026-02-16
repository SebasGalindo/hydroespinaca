using BffService.Domain.Entities;

namespace BffService.Domain.Interfaces;

/// <summary>
/// Contract for session storage used to persist user tokens between requests.
/// </summary>
public interface ISessionRepository
{
    Task<Session?> GetAsync(string sessionId, CancellationToken cancellationToken = default);
    Task SaveAsync(Session session, CancellationToken cancellationToken = default);
    Task DeleteAsync(string sessionId, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string sessionId, CancellationToken cancellationToken = default);
    Task CleanupExpiredSessionsAsync(CancellationToken cancellationToken = default);
}