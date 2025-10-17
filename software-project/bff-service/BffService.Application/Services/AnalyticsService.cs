using BffService.Application.Interfaces;
using BffService.Domain.Constants;
using BffService.Domain.DTOs;
using BffService.Domain.Interfaces;
using BffService.Domain.ValueObjects;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace BffService.Application.Services;

public class AnalyticsService : IAnalyticsService
{
    private readonly IProxyService _proxyService;
    private readonly IMemoryCache _cache;
    private readonly ILogger<AnalyticsService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    private static readonly TimeSpan CacheTTL = TimeSpan.FromMinutes(5);

    public AnalyticsService(
        IProxyService proxyService,
        IMemoryCache cache,
        ILogger<AnalyticsService> logger)
    {
        _proxyService = proxyService;
        _cache = cache;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    public async Task<EnvironmentalAggregatesResponse> GetEnvironmentalAggregatesAsync(
        GetEnvironmentalAggregatesRequest request,
        string? accessToken,
        CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;

        // Validate view parameter - only daily, weekly, monthly allowed
        var validViews = new[] { "daily", "weekly", "monthly" };
        if (!validViews.Contains(request.View.ToLower()))
        {
            throw new InvalidOperationException($"Vista inválida. Valores permitidos: {string.Join(", ", validViews)}");
        }

        // Validate date range
        if (request.StartDate >= request.EndDate)
        {
            throw new InvalidOperationException("La fecha inicial debe ser menor a la fecha final.");
        }

        // Generate cache key based on request parameters
        var cacheKey = $"env_aggregates_{request.StartDate:yyyyMMddHHmmss}_{request.EndDate:yyyyMMddHHmmss}_{request.View.ToLower()}";

        try
        {
            // Try to get from cache
            if (_cache.TryGetValue<EnvironmentalAggregatesResponse>(cacheKey, out var cachedResult))
            {
                var cacheDuration = (DateTime.UtcNow - startTime).TotalMilliseconds;
                _logger.LogInformation(
                    "✅ Cache hit for environmental aggregates. Duration: {Duration}ms, Key: {CacheKey}",
                    cacheDuration, cacheKey);
                return cachedResult!;
            }

            _logger.LogInformation(
                "Cache miss. Requesting environmental aggregates: StartDate={StartDate}, EndDate={EndDate}, View={View}",
                request.StartDate, request.EndDate, request.View);

            var requestBody = JsonSerializer.Serialize(request, _jsonOptions);

            var proxyRequest = new ProxyRequest(
                "POST",
                "/aggregates/environmental",
                new Dictionary<string, string>
                {
                    { "Content-Type", "application/json" }
                },
                requestBody
            );

            var response = await _proxyService.ForwardRequestAsync(
                proxyRequest,
                accessToken,
                BffConstants.Proxy.Services.SensorService,
                cancellationToken
            );

            var duration = (DateTime.UtcNow - startTime).TotalMilliseconds;

            if (response.StatusCode != 200)
            {
                _logger.LogWarning(
                    "Sensor service returned status code {StatusCode} for environmental aggregates. Duration: {Duration}ms",
                    response.StatusCode, duration);

                // Return empty response or throw exception based on status code
                if (response.StatusCode == 400)
                {
                    throw new InvalidOperationException($"Invalid request: {response.Body}");
                }

                return new EnvironmentalAggregatesResponse { Variables = new List<EnvironmentalAggregateResponse>() };
            }

            if (string.IsNullOrEmpty(response.Body))
            {
                _logger.LogWarning("Sensor service returned empty body for environmental aggregates");
                return new EnvironmentalAggregatesResponse { Variables = new List<EnvironmentalAggregateResponse>() };
            }

            var result = JsonSerializer.Deserialize<EnvironmentalAggregatesResponse>(response.Body, _jsonOptions);

            // Store in cache with TTL
            if (result != null && result.Variables != null && result.Variables.Any())
            {
                _cache.Set(cacheKey, result, new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = CacheTTL
                });

                _logger.LogInformation(
                    "Successfully retrieved and cached environmental aggregates. Variables: {Count}, Duration: {Duration}ms, TTL: {TTL}min",
                    result.Variables.Count, duration, CacheTTL.TotalMinutes);
            }
            else
            {
                _logger.LogInformation(
                    "Successfully retrieved environmental aggregates (no data to cache). Duration: {Duration}ms",
                    duration);
            }

            return result ?? new EnvironmentalAggregatesResponse { Variables = new List<EnvironmentalAggregateResponse>() };
        }
        catch (Exception ex)
        {
            var duration = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogError(ex,
                "Error getting environmental aggregates. Duration: {Duration}ms", duration);
            throw;
        }
    }
}
