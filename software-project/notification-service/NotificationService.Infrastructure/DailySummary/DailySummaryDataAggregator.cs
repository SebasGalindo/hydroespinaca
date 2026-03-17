using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using HydroEspinaca.Shared.Authentication.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Interfaces;
using NotificationService.Infrastructure.Options;

namespace NotificationService.Infrastructure.DailySummary;

/// <summary>
/// Aggregates data from sensor-service, actuator-service, fuzzy-service and weather-service
/// for the daily summary notification. Uses M2M authentication for service-to-service calls.
/// </summary>
public class DailySummaryDataAggregator : IDailySummaryDataAggregator
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly M2MTokenService _m2mTokenService;
    private readonly ServiceUrlSettings _serviceUrls;
    private readonly ILogger<DailySummaryDataAggregator> _logger;
    private readonly JsonSerializerOptions _snakeCaseOptions;
    private readonly JsonSerializerOptions _camelCaseOptions;

    public DailySummaryDataAggregator(
        IHttpClientFactory httpClientFactory,
        M2MTokenService m2mTokenService,
        IOptions<ServiceUrlSettings> serviceUrls,
        ILogger<DailySummaryDataAggregator> logger)
    {
        _httpClientFactory = httpClientFactory;
        _m2mTokenService = m2mTokenService;
        _serviceUrls = serviceUrls.Value;
        _logger = logger;
        
        _snakeCaseOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        };
        
        _camelCaseOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    public async Task<DailySummaryData> AggregateAsync(
        string userId, NotificationPreference preference, CancellationToken ct = default)
    {
        // If the user has disabled the entire daily summary, return an empty data object (shouldn't normally happen since we check before scheduling, but just in case)
        var config = preference.DailySummary;
        var now = DateTime.UtcNow;
        var last24h = now.AddHours(-24);

        var data = new DailySummaryData
        {
            Date = last24h.Date,
            UserId = userId,
            IncludeSensorAverages = config?.IncludeSensorAverages ?? true,
            IncludeActuatorRuntime = config?.IncludeActuatorRuntime ?? true,
            IncludeFuzzyRules = config?.IncludeFuzzyRules ?? true,
            IncludeWeatherForecast = config?.IncludeWeatherForecast ?? true
        };

        // Run all enabled data fetches in parallel
        var tasks = new List<Task>();

        if (data.IncludeSensorAverages)
            tasks.Add(FetchSensorData(data, last24h, now, ct));

        if (data.IncludeActuatorRuntime)
            tasks.Add(FetchActuatorData(data, last24h, now, ct));

        if (data.IncludeFuzzyRules)
            tasks.Add(FetchFuzzyData(data, ct));

        if (data.IncludeWeatherForecast)
            tasks.Add(FetchWeatherData(data, ct));

        await Task.WhenAll(tasks);

        return data;
    }

    // ──────────────── Sensor Service ────────────────
    private async Task FetchSensorData(DailySummaryData data, DateTime start, DateTime end, CancellationToken ct)
    { 
        try
        {
            var client = await CreateAuthenticatedClient(ct);
            var requestBody = new { startDate = start, endDate = end, view = "daily" };
            var json = JsonSerializer.Serialize(requestBody, _camelCaseOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PostAsync(
                $"{_serviceUrls.SensorServiceUrl}/api/aggregates/environmental", content, ct);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<SensorAggregateResponse>(_camelCaseOptions, ct);
            if (result?.Variables != null)
            {
                data.SensorSummaries = result.Variables.Select(v => new SensorVariableSummary
                {
                    VariableCode = v.VariableCode ?? "",
                    VariableName = v.VariableName ?? "",
                    Min = v.Summary?.Min ?? 0,
                    Max = v.Summary?.Max ?? 0,
                    Avg = v.Summary?.Avg ?? 0,
                    Count = v.Summary?.Count ?? 0
                }).ToList();
            }

            _logger.LogDebug("Fetched {Count} sensor variables for daily summary", data.SensorSummaries.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch sensor data for daily summary — section will be empty");
        }
    }

    // ──────────────── Actuator Service ────────────────

    private async Task FetchActuatorData(DailySummaryData data, DateTime start, DateTime end, CancellationToken ct)
    {
        try
        {
            var client = await CreateAuthenticatedClient(ct);
            var requestBody = new { startDate = start, endDate = end, view = "daily" };
            var json = JsonSerializer.Serialize(requestBody, _camelCaseOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PostAsync(
                $"{_serviceUrls.ActuatorServiceUrl}/api/commands/analytics", content, ct);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<ActuatorAnalyticsResponse>(_camelCaseOptions, ct);
            if (result != null)
            {
                var durations = result.TotalDurationByActuator ?? [];
                var proportions = (result.ActiveTimeProportion ?? [])
                    .ToDictionary(p => p.ActuatorCode ?? "", p => p.Percentage);

                data.ActuatorSummaries = durations.Select(d => new ActuatorRuntimeSummary
                {
                    ActuatorCode = d.ActuatorCode ?? "",
                    TotalDurationMinutes = Math.Round(d.TotalDurationSeconds / 60.0, 1),
                    ActivationCount = d.ActivationCount,
                    Percentage = proportions.GetValueOrDefault(d.ActuatorCode ?? "", 0)
                }).ToList();
            }

            _logger.LogDebug("Fetched {Count} actuator summaries for daily summary", data.ActuatorSummaries.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch actuator data for daily summary — section will be empty");
        }
    }

    // ──────────────── Fuzzy Service ────────────────

    private async Task FetchFuzzyData(DailySummaryData data, CancellationToken ct)
    {
        try
        {
            var client = await CreateAuthenticatedClient(ct);

            var response = await client.GetAsync(
                $"{_serviceUrls.FuzzyServiceUrl}/api/fuzzy-evaluations/recent?hours=24&page_size=100", ct);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<FuzzyEvaluationsResponse>(_snakeCaseOptions, ct);
            if (result?.Evaluations != null)
            {
                var evaluations = result.Evaluations;

                // Count rule activations across all evaluations
                var ruleActivations = evaluations
                    .SelectMany(e => e.ActivatedRules ?? [])
                    .GroupBy(r => r.RuleId)
                    .Select(g => new TopRuleSummary
                    {
                        RuleId = g.Key ?? "",
                        ActivationCount = g.Count(),
                        AvgFiringStrength = Math.Round(g.Average(r => r.FiringStrength), 3)
                    })
                    .OrderByDescending(r => r.ActivationCount)
                    .Take(5)
                    .ToList();

                data.FuzzyEvaluation = new FuzzyEvaluationSummary
                {
                    EvaluationCount = result.TotalCount,
                    SystemName = await ResolveSystemName(client, evaluations.FirstOrDefault()?.SystemId, ct),
                    TopRules = ruleActivations
                };
            }

            _logger.LogDebug("Fetched {Count} fuzzy evaluations for daily summary",
                data.FuzzyEvaluation?.EvaluationCount ?? 0);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch fuzzy data for daily summary — section will be empty");
        }
    }

    // ──────────────── Weather Service ────────────────

    /// <summary>
    /// Resolves a fuzzy system ID to its human-readable name via the fuzzy-service API.
    /// Falls back to the raw ID if the lookup fails.
    /// </summary>
    private async Task<string?> ResolveSystemName(HttpClient client, string? systemId, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(systemId)) return null;

        try
        {
            var response = await client.GetAsync(
                $"{_serviceUrls.FuzzyServiceUrl}/api/fuzzy-systems/{systemId}", ct);

            if (response.IsSuccessStatusCode)
            {
                var system = await response.Content.ReadFromJsonAsync<FuzzySystemItem>(_snakeCaseOptions, ct);
                if (!string.IsNullOrEmpty(system?.Name))
                    return system.Name;
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Could not resolve fuzzy system name for {SystemId}", systemId);
        }

        return systemId; // Fallback to ID if name lookup fails
    }

    private async Task FetchWeatherData(DailySummaryData data, CancellationToken ct)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("DailySummary");

            // Weather service is AllowAnonymous — no M2M token needed
            var response = await client.GetAsync(
                $"{_serviceUrls.WeatherServiceUrl}/api/weather/forecast/daily", ct);
            response.EnsureSuccessStatusCode();

            var forecasts = await response.Content.ReadFromJsonAsync<List<DailyForecastItem>>(_camelCaseOptions, ct);
            // Tomorrow's forecast (index 1 = tomorrow)
            var tomorrow = forecasts?.ElementAtOrDefault(1);
            if (tomorrow != null)
            {
                data.WeatherForecast = new WeatherForecastSummary
                {
                    TempMin = tomorrow.TempMin,
                    TempMax = tomorrow.TempMax,
                    Pop = Math.Round(tomorrow.Pop * 100, 0),
                    Description = tomorrow.Description ?? "",
                    Summary = tomorrow.Summary
                };
            }

            _logger.LogDebug("Fetched weather forecast for daily summary");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch weather data for daily summary — section will be empty");
        }
    }

    // ──────────────── Helpers ────────────────

    private async Task<HttpClient> CreateAuthenticatedClient(CancellationToken ct)
    {
        var client = _httpClientFactory.CreateClient("DailySummary");
        await _m2mTokenService.ConfigureHttpClientAsync(client, ct);
        return client;
    }

    // ──────────────── Response DTOs (internal) ────────────────

    private class SensorAggregateResponse
    {
        public List<SensorVariable>? Variables { get; set; }
    }

    private class SensorVariable
    {
        public string? VariableCode { get; set; }
        public string? VariableName { get; set; }
        public SensorSummaryBlock? Summary { get; set; }
    }

    private class SensorSummaryBlock
    {
        public double Min { get; set; }
        public double Max { get; set; }
        public double Avg { get; set; }
        public int Count { get; set; }
    }

    private class ActuatorAnalyticsResponse
    {
        public List<ActuatorDurationItem>? TotalDurationByActuator { get; set; }
        public List<ActuatorProportionItem>? ActiveTimeProportion { get; set; }
    }

    private class ActuatorDurationItem
    {
        public string? ActuatorCode { get; set; }
        public double TotalDurationSeconds { get; set; }
        public int ActivationCount { get; set; }
    }

    private class ActuatorProportionItem
    {
        public string? ActuatorCode { get; set; }
        public double Percentage { get; set; }
    }

    private class FuzzyEvaluationsResponse
    {
        public List<FuzzyEvaluationItem>? Evaluations { get; set; }
        public int TotalCount { get; set; }
    }

    private class FuzzyEvaluationItem
    {
        public string? SystemId { get; set; }
        public List<RuleActivation>? ActivatedRules { get; set; }
    }

    private class FuzzySystemItem
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
    }

    private class RuleActivation
    {
        public string? RuleId { get; set; }
        public double FiringStrength { get; set; }
    }

    private class DailyForecastItem
    {
        public double TempMin { get; set; }
        public double TempMax { get; set; }
        public double Pop { get; set; }
        public string? Description { get; set; }
        public string? Summary { get; set; }
    }
}
