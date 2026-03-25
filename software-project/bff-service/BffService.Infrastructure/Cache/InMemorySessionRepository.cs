using BffService.Domain.Entities;
using BffService.Domain.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using BffService.Domain.Constants;
using Microsoft.Extensions.Configuration;

namespace BffService.Infrastructure.Cache;

/// <summary>
/// In-memory session repository using ConcurrentDictionary for storing user sessions.
/// </summary>
public class InMemorySessionRepository : ISessionRepository
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<InMemorySessionRepository> _logger;
    private readonly MemoryCacheEntryOptions _defaultOptions;

    public InMemorySessionRepository(IMemoryCache cache, ILogger<InMemorySessionRepository> logger, IConfiguration configuration)
    {
        _cache = cache;
        _logger = logger;

        var accessTokenMinutes = configuration.GetValue<int>(BffConstants.Sessions.AccessTokenExpiryMinutesConfigKey, 60);
        var refreshTokenDays = configuration.GetValue<int>(BffConstants.Sessions.RefreshTokenExpiryDaysConfigKey, 7);

        // Cache configuration:
        // - SlidingExpiration: Based on refresh token duration (NOT access token)
        //   This ensures the session stays in cache as long as the user is active within the refresh token window
        // - AbsoluteExpiration: Maximum lifetime equals refresh token expiry
        //   This ensures sessions are eventually cleaned up even if continuously active
        _defaultOptions = new MemoryCacheEntryOptions
        {
            SlidingExpiration = TimeSpan.FromDays(refreshTokenDays),
            AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(refreshTokenDays),
            Priority = CacheItemPriority.High
        };

        _logger.LogInformation(
            "Session cache configured | SlidingExpiration: {SlidingDays} days | AbsoluteExpiration: {AbsoluteDays} days",
            refreshTokenDays,
            refreshTokenDays
        );
    }

    public Task<Session?> GetAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        try
        {
            var cacheKey = GetCacheKey(sessionId);
            var found = _cache.TryGetValue(cacheKey, out Session? session);

            if (found && session != null)
            {
                _logger.LogDebug(
                    "Retrieved session {SessionId} from cache: True | ExpiresAt: {ExpiresAt} | RefreshExpiresAt: {RefreshExpiresAt}",
                    sessionId,
                    session.ExpiresAt,
                    session.RefreshTokenExpiresAt
                );
            }
            else
            {
                _logger.LogWarning(
                    "⚠️  Session {SessionId} NOT FOUND in cache - may have been evicted due to inactivity or expiration",
                    sessionId
                );
            }

            return Task.FromResult(session);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving session {SessionId} from cache", sessionId);
            throw;
        }
    }

    public Task SaveAsync(Session session, CancellationToken cancellationToken = default)
    {
        try
        {
            var cacheKey = GetCacheKey(session.SessionId);
            _cache.Set(cacheKey, session, _defaultOptions);
            _logger.LogDebug(
                "Saved session {SessionId} to cache | ExpiresAt: {ExpiresAt} | RefreshTokenExpiresAt: {RefreshExpiresAt}",
                session.SessionId,
                session.ExpiresAt,
                session.RefreshTokenExpiresAt
            );
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving session {SessionId} to cache", session.SessionId);
            throw;
        }
    }

    public Task DeleteAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        try
        {
            var cacheKey = GetCacheKey(sessionId);
            _cache.Remove(cacheKey);
            _logger.LogDebug("Deleted session {SessionId} from cache", sessionId);
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting session {SessionId} from cache", sessionId);
            throw;
        }
    }

    public Task<bool> ExistsAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        try
        {
            var exists = _cache.TryGetValue(GetCacheKey(sessionId), out _);
            _logger.LogDebug("Checked existence of session {SessionId}: {Exists}", sessionId, exists);
            return Task.FromResult(exists);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking existence of session {SessionId}", sessionId);
            throw;
        }
    }

    public Task CleanupExpiredSessionsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Cleanup of expired sessions is handled automatically by MemoryCache");
        return Task.CompletedTask;
    }

    private static string GetCacheKey(string sessionId)
    {
        return $"session:{sessionId}";
    }
}