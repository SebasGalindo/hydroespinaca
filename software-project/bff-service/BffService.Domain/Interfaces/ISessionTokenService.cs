using BffService.Domain.Entities;

namespace BffService.Domain.Interfaces;

/// <summary>
/// Service for managing session tokens including automatic refresh
/// </summary>
public interface ISessionTokenService
{
    /// <summary>
    /// Gets a session with valid access tokens, automatically refreshing if needed
    /// </summary>
    /// <param name="sessionId">The session identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Session with valid access tokens</returns>
    /// <exception cref="Exceptions.SessionNotFoundException">Thrown when session is not found</exception>
    /// <exception cref="Exceptions.InvalidTokenException">Thrown when session tokens are invalid</exception>
    /// <exception cref="Exceptions.SessionExpiredException">Thrown when session is expired and cannot be refreshed</exception>
    Task<Session> GetSessionWithValidTokensAsync(string sessionId, CancellationToken cancellationToken);
}
