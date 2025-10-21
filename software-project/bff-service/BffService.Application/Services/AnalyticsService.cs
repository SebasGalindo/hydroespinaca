using BffService.Application.Interfaces;
using BffService.Domain.Constants;
using BffService.Domain.DTOs;
using BffService.Domain.Interfaces;
using BffService.Domain.ValueObjects;
using HydroEspinaca.Shared.DTOs.Analytics;
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
        EnvironmentalAnalyticsRequest request,
        string? accessToken,
        CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;

        // Validate view parameter - only hourly, daily, weekly, monthly allowed
        var validViews = new[] { "hourly", "daily", "weekly", "monthly" };
        var viewLower = request.View.ToLower();
        if (!validViews.Contains(viewLower))
        {
            throw new InvalidOperationException($"Vista inválida '{request.View}'. Valores permitidos: {string.Join(", ", validViews)}");
        }

        // Validate date range - allow same day for any view
        if (request.StartDate > request.EndDate)
        {
            throw new InvalidOperationException("La fecha inicial no puede ser mayor a la fecha final.");
        }

        // Validate maximum date range based on view (date comparison only)
        var daysDifference = (request.EndDate.Date - request.StartDate.Date).Days;
        var maxDaysAllowed = viewLower switch
        {
            "hourly" => 1,    // Max 1 day
            "daily" => 14,    // Max 14 days
            "weekly" => 84,   // Max 12 weeks
            "monthly" => 365, // Max 12 months
            _ => 14
        };

        if (daysDifference > maxDaysAllowed)
        {
            var viewLabel = viewLower switch
            {
                "hourly" => "horaria (máximo 1 día)",
                "daily" => "diaria (máximo 14 días)",
                "weekly" => "semanal (máximo 84 días)",
                "monthly" => "mensual (máximo 365 días)",
                _ => $"{viewLower} (máximo {maxDaysAllowed} días)"
            };
            throw new InvalidOperationException($"Rango de fechas inválido para vista {viewLabel}. Días seleccionados: {daysDifference}");
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

    public async Task<ActuatorAnalyticsResponse> GetActuatorAnalyticsAsync(
        ActuatorAnalyticsRequest request,
        string? accessToken,
        CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;

        // Validate view parameter - only hourly, daily, weekly, monthly allowed
        var validViews = new[] { "hourly", "daily", "weekly", "monthly" };
        var viewLower = request.View.ToLower();
        if (!validViews.Contains(viewLower))
        {
            throw new InvalidOperationException($"Vista inválida '{request.View}'. Valores permitidos: {string.Join(", ", validViews)}");
        }

        // Validate date range
        if (request.StartDate > request.EndDate)
        {
            throw new InvalidOperationException("La fecha inicial no puede ser mayor a la fecha final.");
        }

        // Generate cache key based on request parameters
        var cacheKey = $"actuator_analytics_{request.StartDate:yyyyMMddHHmmss}_{request.EndDate:yyyyMMddHHmmss}_{request.View.ToLower()}";

        try
        {
            // Try to get from cache
            if (_cache.TryGetValue<ActuatorAnalyticsResponse>(cacheKey, out var cachedResult))
            {
                var cacheDuration = (DateTime.UtcNow - startTime).TotalMilliseconds;
                _logger.LogInformation(
                    "✅ Cache hit for actuator analytics. Duration: {Duration}ms, Key: {CacheKey}",
                    cacheDuration, cacheKey);
                return cachedResult!;
            }

            _logger.LogInformation(
                "Cache miss. Requesting actuator analytics: StartDate={StartDate}, EndDate={EndDate}, View={View}",
                request.StartDate, request.EndDate, request.View);

            var requestBody = JsonSerializer.Serialize(request, _jsonOptions);

            var proxyRequest = new ProxyRequest(
                "POST",
                "/commands/analytics",
                new Dictionary<string, string>
                {
                    { "Content-Type", "application/json" }
                },
                requestBody
            );

            var response = await _proxyService.ForwardRequestAsync(
                proxyRequest,
                accessToken,
                BffConstants.Proxy.Services.ActuatorService,
                cancellationToken
            );

            var duration = (DateTime.UtcNow - startTime).TotalMilliseconds;

            if (response.StatusCode != 200)
            {
                _logger.LogWarning(
                    "Actuator service returned status code {StatusCode} for actuator analytics. Duration: {Duration}ms",
                    response.StatusCode, duration);

                if (response.StatusCode == 400)
                {
                    throw new InvalidOperationException($"Invalid request: {response.Body}");
                }

                return new ActuatorAnalyticsResponse
                {
                    Timeline = new List<ActuatorTimelineItem>(),
                    TotalDurationByActuator = new List<ActuatorTotalDurationItem>(),
                    ActiveTimeProportion = new List<ActuatorActiveTimeProportionItem>()
                };
            }

            if (string.IsNullOrEmpty(response.Body))
            {
                _logger.LogWarning("Actuator service returned empty body for actuator analytics");
                return new ActuatorAnalyticsResponse
                {
                    Timeline = new List<ActuatorTimelineItem>(),
                    TotalDurationByActuator = new List<ActuatorTotalDurationItem>(),
                    ActiveTimeProportion = new List<ActuatorActiveTimeProportionItem>()
                };
            }

            var result = JsonSerializer.Deserialize<ActuatorAnalyticsResponse>(response.Body, _jsonOptions);

            // Store in cache with TTL
            if (result != null && (result.Timeline.Any() || result.TotalDurationByActuator.Any()))
            {
                _cache.Set(cacheKey, result, new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = CacheTTL
                });

                _logger.LogInformation(
                    "Successfully retrieved and cached actuator analytics. Timeline: {TimelineCount}, Duration: {Duration}ms, TTL: {TTL}min",
                    result.Timeline.Count, duration, CacheTTL.TotalMinutes);
            }
            else
            {
                _logger.LogInformation(
                    "Successfully retrieved actuator analytics (no data to cache). Duration: {Duration}ms",
                    duration);
            }

            return result ?? new ActuatorAnalyticsResponse
            {
                Timeline = new List<ActuatorTimelineItem>(),
                TotalDurationByActuator = new List<ActuatorTotalDurationItem>(),
                ActiveTimeProportion = new List<ActuatorActiveTimeProportionItem>()
            };
        }
        catch (Exception ex)
        {
            var duration = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogError(ex,
                "Error getting actuator analytics. Duration: {Duration}ms", duration);
            throw;
        }
    }
}
