using BffService.Application.Interfaces;
using BffService.Application.Helpers;
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
    private readonly ILogger<AnalyticsService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly AnalyticsCacheHelper<EnvironmentalAggregatesResponse> _envCacheHelper;
    private readonly AnalyticsCacheHelper<ActuatorAnalyticsResponse> _actuatorCacheHelper;
    private static readonly TimeSpan CacheTTL = TimeSpan.FromMinutes(5);

    public AnalyticsService(
        IProxyService proxyService,
        IMemoryCache cache,
        ILogger<AnalyticsService> logger)
    {
        _proxyService = proxyService;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
        _envCacheHelper = new AnalyticsCacheHelper<EnvironmentalAggregatesResponse>(cache, logger, CacheTTL);
        _actuatorCacheHelper = new AnalyticsCacheHelper<ActuatorAnalyticsResponse>(cache, logger, CacheTTL);
    }

    public async Task<EnvironmentalAggregatesResponse> GetEnvironmentalAggregatesAsync(
        EnvironmentalAnalyticsRequest request,
        string? accessToken,
        CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;

        // Validate request parameters using shared validator
        AnalyticsRequestValidator.ValidateRequest(request.View, request.StartDate, request.EndDate);

        // Generate cache key
        var cacheKey = AnalyticsCacheHelper<EnvironmentalAggregatesResponse>.GenerateCacheKey(
            "env_aggregates", request.StartDate, request.EndDate, request.View);

        try
        {
            // Try to get from cache
            if (_envCacheHelper.TryGetCached(cacheKey, out var cachedResult, startTime))
            {
                return cachedResult!;
            }

            _logger.LogInformation(
                "Requesting environmental aggregates: StartDate={StartDate}, EndDate={EndDate}, View={View}",
                request.StartDate, request.EndDate, request.View);

            // Build and send proxy request
            var requestBody = JsonSerializer.Serialize(request, _jsonOptions);
            var proxyRequest = new ProxyRequest(
                "POST",
                "/aggregates/environmental",
                new Dictionary<string, string> { { "Content-Type", "application/json" } },
                requestBody
            );

            var response = await _proxyService.ForwardRequestAsync(
                proxyRequest,
                accessToken,
                BffConstants.Proxy.Services.SensorService,
                cancellationToken
            );

            // Handle response using shared helper
            var result = AnalyticsProxyHelper.HandleProxyResponse(
                response,
                _logger,
                "environmental aggregates",
                () => new EnvironmentalAggregatesResponse { Variables = new List<EnvironmentalAggregateResponse>() },
                _jsonOptions,
                startTime
            );

            // Cache the result
            _envCacheHelper.SetCache(
                cacheKey,
                result,
                startTime,
                r => r.Variables != null && r.Variables.Count > 0
            );

            return result;
        }
        catch (Exception ex)
        {
            var duration = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogError(ex, "Error getting environmental aggregates. Duration: {Duration}ms", duration);
            throw;
        }
    }

    public async Task<ActuatorAnalyticsResponse> GetActuatorAnalyticsAsync(
        ActuatorAnalyticsRequest request,
        string? accessToken,
        CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;

        // Validate request parameters using shared validator
        AnalyticsRequestValidator.ValidateRequest(request.View, request.StartDate, request.EndDate);

        // Generate cache key
        var cacheKey = AnalyticsCacheHelper<ActuatorAnalyticsResponse>.GenerateCacheKey(
            "actuator_analytics", request.StartDate, request.EndDate, request.View);

        try
        {
            // Try to get from cache
            if (_actuatorCacheHelper.TryGetCached(cacheKey, out var cachedResult, startTime))
            {
                return cachedResult!;
            }

            _logger.LogInformation(
                "Requesting actuator analytics: StartDate={StartDate}, EndDate={EndDate}, View={View}",
                request.StartDate, request.EndDate, request.View);

            // Build and send proxy request
            var requestBody = JsonSerializer.Serialize(request, _jsonOptions);
            var proxyRequest = new ProxyRequest(
                "POST",
                "/commands/analytics",
                new Dictionary<string, string> { { "Content-Type", "application/json" } },
                requestBody
            );

            var response = await _proxyService.ForwardRequestAsync(
                proxyRequest,
                accessToken,
                BffConstants.Proxy.Services.ActuatorService,
                cancellationToken
            );

            // Handle response using shared helper
            var result = AnalyticsProxyHelper.HandleProxyResponse(
                response,
                _logger,
                "actuator analytics",
                () => new ActuatorAnalyticsResponse
                {
                    Timeline = new List<ActuatorTimelineItem>(),
                    TotalDurationByActuator = new List<ActuatorTotalDurationItem>(),
                    ActiveTimeProportion = new List<ActuatorActiveTimeProportionItem>()
                },
                _jsonOptions,
                startTime
            );

            // Cache the result
            _actuatorCacheHelper.SetCache(
                cacheKey,
                result,
                startTime,
                r => r.Timeline.Count > 0 || r.TotalDurationByActuator.Count > 0
            );

            return result;
        }
        catch (Exception ex)
        {
            var duration = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogError(ex, "Error getting actuator analytics. Duration: {Duration}ms", duration);
            throw;
        }
    }
}
