using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace BffService.Application.Helpers;

/// <summary>
/// Helper class to manage caching logic for analytics data.
/// Reduces code duplication across analytics services.
/// </summary>
public class AnalyticsCacheHelper<T> where T : class
{
    private readonly IMemoryCache _cache;
    private readonly ILogger _logger;
    private readonly TimeSpan _cacheTTL;

    public AnalyticsCacheHelper(IMemoryCache cache, ILogger logger, TimeSpan cacheTTL)
    {
        _cache = cache;
        _logger = logger;
        _cacheTTL = cacheTTL;
    }

    /// <summary>
    /// Attempts to retrieve a cached result. Returns true if found.
    /// </summary>
    public bool TryGetCached(string cacheKey, out T? result, DateTime startTime)
    {
        if (_cache.TryGetValue<T>(cacheKey, out result) && result != null)
        {
            var cacheDuration = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogInformation(
                "✅ Cache hit. Duration: {Duration}ms, Key: {CacheKey}",
                cacheDuration, cacheKey);
            return true;
        }

        _logger.LogInformation("Cache miss. Key: {CacheKey}", cacheKey);
        return false;
    }

    /// <summary>
    /// Stores a result in the cache with the configured TTL.
    /// </summary>
    public void SetCache(string cacheKey, T result, DateTime startTime, Func<T, bool> hasDataPredicate)
    {
        var duration = (DateTime.UtcNow - startTime).TotalMilliseconds;

        if (hasDataPredicate(result))
        {
            _cache.Set(cacheKey, result, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = _cacheTTL
            });

            _logger.LogInformation(
                "Successfully retrieved and cached data. Duration: {Duration}ms, TTL: {TTL}min",
                duration, _cacheTTL.TotalMinutes);
        }
        else
        {
            _logger.LogInformation(
                "Successfully retrieved data (no data to cache). Duration: {Duration}ms",
                duration);
        }
    }

    /// <summary>
    /// Generates a cache key based on request parameters.
    /// </summary>
    public static string GenerateCacheKey(string prefix, DateTime startDate, DateTime endDate, string view)
    {
        return $"{prefix}_{startDate:yyyyMMddHHmmss}_{endDate:yyyyMMddHHmmss}_{view.ToLower()}";
    }
}

/// <summary>
/// Helper class for making proxied requests with consistent error handling.
/// </summary>
public static class AnalyticsProxyHelper
{
    /// <summary>
    /// Handles the response from a proxy request, logging and validating status codes.
    /// </summary>
    public static T HandleProxyResponse<T>(
        BffService.Domain.ValueObjects.ProxyResponse response,
        ILogger logger,
        string operationName,
        Func<T> emptyResultFactory,
        JsonSerializerOptions jsonOptions,
        DateTime startTime) where T : class
    {
        var duration = (DateTime.UtcNow - startTime).TotalMilliseconds;

        if (response.StatusCode != 200)
        {
            logger.LogWarning(
                "Service returned status code {StatusCode} for {Operation}. Duration: {Duration}ms",
                response.StatusCode, operationName, duration);

            if (response.StatusCode == 400)
            {
                throw new InvalidOperationException($"Invalid request: {response.Body}");
            }

            return emptyResultFactory();
        }

        if (string.IsNullOrEmpty(response.Body))
        {
            logger.LogWarning("Service returned empty body for {Operation}", operationName);
            return emptyResultFactory();
        }

        var result = JsonSerializer.Deserialize<T>(response.Body, jsonOptions);
        return result ?? emptyResultFactory();
    }
}
