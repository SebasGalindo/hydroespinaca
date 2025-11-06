using BffService.Application.Interfaces;
using BffService.Domain.Entities;
using BffService.Domain.Exceptions;
using BffService.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.IdentityModel.Tokens.Jwt;
using System.Text.Json;

namespace BffService.Application.Services;

/// <summary>
/// Service for managing session tokens including automatic refresh
/// </summary>
public class SessionTokenService : ISessionTokenService
{
    private readonly ISessionService _sessionService;
    private readonly IAuthService _authService;
    private readonly ILogger<SessionTokenService> _logger;

    /// <summary>
    /// Static dictionary to maintain per-session locks to prevent concurrent token refresh operations
    /// across multiple service instances (required because SessionTokenService is registered as Scoped).
    /// This ensures that concurrent HTTP requests attempting to refresh the same session will be serialized.
    /// </summary>
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> _refreshLocks = new();

    public SessionTokenService(
        ISessionService sessionService,
        IAuthService authService,
        ILogger<SessionTokenService> logger)
    {
        _sessionService = sessionService;
        _authService = authService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Session> GetSessionWithValidTokensAsync(string sessionId, CancellationToken cancellationToken)
    {
        try
        {
            // Get the full session with all token information
            var session = await _sessionService.GetFullSessionAsync(sessionId, cancellationToken);
            if (session == null)
            {
                _logger.LogWarning("Session not found: {SessionId}", sessionId);
                throw new SessionNotFoundException(sessionId);
            }

            // Check if the access token is expired or expiring soon (proactive refresh)
            if (session.IsExpiringSoon())
            {
                if (!session.CanRefresh())
                {
                    _logger.LogWarning("Session expired/expiring and cannot be refreshed: {SessionId}", sessionId);
                    throw new SessionExpiredException(sessionId);
                }

                // Get or create a semaphore for this session to prevent concurrent refresh operations
                var semaphore = _refreshLocks.GetOrAdd(sessionId, _ => new SemaphoreSlim(1, 1));

                _logger.LogDebug("Acquiring lock for token refresh (proactive): {SessionId}", sessionId);

                // Wait for exclusive access to refresh this session's token
                await semaphore.WaitAsync(cancellationToken);
                try
                {
                    _logger.LogDebug("Lock acquired for session: {SessionId}", sessionId);

                    // Re-check if token is still expired after acquiring the lock
                    // Another thread might have already refreshed it while we were waiting
                    session = await _sessionService.GetFullSessionAsync(sessionId, cancellationToken);

                    if (session == null)
                    {
                        _logger.LogWarning("Session not found after acquiring lock: {SessionId}", sessionId);
                        throw new SessionNotFoundException(sessionId);
                    }

                    // Double-check: only refresh if still expiring soon (another thread might have already refreshed it)
                    if (session.IsExpiringSoon())
                    {
                        if (!session.CanRefresh())
                        {
                            _logger.LogWarning("Session expired/expiring and cannot be refreshed after lock: {SessionId}", sessionId);
                            throw new SessionExpiredException(sessionId);
                        }

                        var timeToExpiry = session.ExpiresAt - DateTime.UtcNow;
                        _logger.LogInformation("Refreshing token for session: {SessionId} (expires in {Seconds}s)", sessionId, timeToExpiry.TotalSeconds);

                        try
                        {
                            // Use the auth service to refresh the token
                            var newTokenInfo = await _authService.RefreshTokenAsync(session.RefreshToken, sessionId, cancellationToken);

                            // Update the session with the new tokens
                            session.UpdateAccessToken(newTokenInfo.AccessToken, newTokenInfo.ExpiresAt);

                            // If we got a new refresh token, update that too
                            if (!string.IsNullOrEmpty(newTokenInfo.RefreshToken))
                            {
                                session.SetTokens(
                                    newTokenInfo.AccessToken,
                                    newTokenInfo.RefreshToken,
                                    newTokenInfo.ExpiresAt,
                                    newTokenInfo.RefreshTokenExpiresAt
                                );
                            }

                            // Save the updated session
                            await _sessionService.UpdateSessionAsync(session, cancellationToken);

                            _logger.LogInformation("Token refreshed successfully for session: {SessionId}", sessionId);
                        }
                        catch (InvalidTokenException ex)
                        {
                            // Refresh token is permanently invalid (expired, revoked, or auth-service rejected it)
                            _logger.LogWarning(ex, "Refresh token is invalid for session {SessionId} - deleting session from cache to prevent retry loops", sessionId);

                            // Delete the session from cache to prevent infinite retry loops
                            try
                            {
                                await _sessionService.DeleteSessionAsync(sessionId, cancellationToken);
                                _logger.LogInformation("Session {SessionId} removed from cache after refresh failure", sessionId);
                            }
                            catch (Exception deleteEx)
                            {
                                _logger.LogError(deleteEx, "Failed to delete session {SessionId} from cache after refresh failure", sessionId);
                            }

                            // Re-throw as SessionExpiredException to trigger proper cleanup in controller
                            throw new SessionExpiredException(sessionId);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Unexpected error refreshing token for session {SessionId}", sessionId);
                            throw;
                        }
                    }
                    else
                    {
                        _logger.LogDebug("Token already refreshed by another thread for session: {SessionId}", sessionId);
                    }
                }
                finally
                {
                    semaphore.Release();
                    _logger.LogDebug("Lock released for session: {SessionId}", sessionId);

                    // Clean up the semaphore from the dictionary to prevent memory leaks
                    // Only remove if no other threads are waiting
                    if (semaphore.CurrentCount == 1)
                    {
                        _refreshLocks.TryRemove(sessionId, out _);
                        _logger.LogTrace("Semaphore cleaned up for session: {SessionId}", sessionId);
                    }
                }
            }

            // Validate tokens only if not expired or after successful refresh
            if (string.IsNullOrEmpty(session.AccessToken))
            {
                _logger.LogError("Session has invalid tokens: {SessionId}", sessionId);
                throw new InvalidTokenException("Session tokens are invalid");
            }

            return session;
        }
        catch (SessionExpiredException)
        {
            // Session has truly expired (7 days from login), remove from cache
            _logger.LogWarning("Session {SessionId} expired and cannot be refreshed, removing from cache", sessionId);
            try
            {
                await _sessionService.DeleteSessionAsync(sessionId, cancellationToken);
            }
            catch (Exception deleteEx)
            {
                _logger.LogError(deleteEx, "Failed to delete expired session {SessionId} from cache", sessionId);
            }
            throw; // Re-throw to trigger cookie cleanup in controller
        }
        catch (Exception ex) when (ex is not SessionNotFoundException && ex is not InvalidTokenException)
        {
            _logger.LogError(ex, "Error validating session tokens for {SessionId}", sessionId);
            throw;
        }
    }
}
