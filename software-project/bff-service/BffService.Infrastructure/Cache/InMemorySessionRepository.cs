using BffService.Domain.Entities;
using BffService.Domain.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using BffService.Domain.Constants;
using Microsoft.Extensions.Configuration;

namespace BffService.Infrastructure.Cache;

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
        
        _defaultOptions = new MemoryCacheEntryOptions
        {
            SlidingExpiration = TimeSpan.FromMinutes(accessTokenMinutes),
            AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(refreshTokenDays),
            Priority = CacheItemPriority.High
        };
    }

    public Task<Session?> GetAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        try
        {
            _cache.TryGetValue(GetCacheKey(sessionId), out Session? session);
            _logger.LogDebug("Retrieved session {SessionId} from cache: {Found}", sessionId, session != null);
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
            _logger.LogDebug("Saved session {SessionId} to cache", session.SessionId);
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