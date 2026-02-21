using BffService.Application.Interfaces;
using BffService.Domain.DTOs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BffService.Infrastructure.Services;

/// <summary>
/// HTTP client for weather-service microservice.
/// Replaces direct OpenWeather API calls — weather-service now owns the OpenWeather integration.
/// Maps weather-service responses to BFF DTOs for backward compatibility.
/// </summary>
public class WeatherServiceClient : IWeatherServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<WeatherServiceClient> _logger;
    private readonly string _baseUrl;
    private readonly JsonSerializerOptions _jsonOptions;

    /// <summary>
    /// Colombia (Bogotá/Mosquera) is UTC-5 = -18000 seconds
    /// </summary>
    private const int TimezoneOffsetSeconds = -18000;

    public WeatherServiceClient(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<WeatherServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _baseUrl = configuration["Services:WeatherService:Url"]
            ?? "http://weather-service:8080";
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
    }

    /// <inheritdoc />
    public async Task<WeatherDto> GetCurrentWeatherAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Calling weather-service GET /api/weather/current");

            var response = await _httpClient.GetAsync($"{_baseUrl}/api/weather/current", cancellationToken);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var currentWeather = JsonSerializer.Deserialize<ForecastCurrentDto>(content, _jsonOptions);

            if (currentWeather == null)
            {
                throw new InvalidOperationException("Failed to deserialize weather-service current weather response");
            }

            // Map to existing WeatherDto for backward compatibility with frontend
            return MapToWeatherDto(currentWeather);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling weather-service /api/weather/current");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<ForecastResponseDto> GetForecastAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Calling weather-service GET /api/weather/forecast");

            var response = await _httpClient.GetAsync($"{_baseUrl}/api/weather/forecast", cancellationToken);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var forecast = JsonSerializer.Deserialize<ForecastResponseDto>(content, _jsonOptions);

            if (forecast == null)
            {
                throw new InvalidOperationException("Failed to deserialize weather-service forecast response");
            }

            return forecast;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling weather-service /api/weather/forecast");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<List<DailyForecastItemDto>> GetDailyForecastAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Calling weather-service GET /api/weather/forecast/daily");

            var response = await _httpClient.GetAsync($"{_baseUrl}/api/weather/forecast/daily", cancellationToken);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var daily = JsonSerializer.Deserialize<List<DailyForecastItemDto>>(content, _jsonOptions);

            return daily ?? new List<DailyForecastItemDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling weather-service /api/weather/forecast/daily");
            throw;
        }
    }

    /// <summary>
    /// Maps the new ForecastCurrentDto from weather-service to the existing WeatherDto
    /// for backward compatibility with the frontend (web and mobile).
    /// </summary>
    private static WeatherDto MapToWeatherDto(ForecastCurrentDto current)
    {
        return new WeatherDto
        {
            Temperature = current.Temperature,
            FeelsLike = current.FeelsLike,
            Humidity = current.Humidity,
            Main = current.Main,
            Description = current.Description,
            Icon = current.Icon,
            WindSpeed = current.WindSpeed,
            Cloudiness = current.Cloudiness,
            Rain1h = current.Rain1h,
            // Convert UTC times to local (UTC-5) for backward compatibility
            Sunrise = current.Sunrise.AddSeconds(TimezoneOffsetSeconds),
            Sunset = current.Sunset.AddSeconds(TimezoneOffsetSeconds),
            LastUpdate = current.LastUpdate.AddSeconds(TimezoneOffsetSeconds)
        };
    }

    // --- Alert config endpoints ---

    /// <inheritdoc />
    public async Task<WeatherAlertConfigDto?> GetAlertConfigAsync(
        string fuzzySystemId, CancellationToken cancellationToken = default)
    {
        try
        {
            var url = $"{_baseUrl}/api/weather/alerts/config/{Uri.EscapeDataString(fuzzySystemId)}";
            var response = await _httpClient.GetAsync(url, cancellationToken);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return null;

            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<WeatherAlertConfigDto>(content, _jsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching alert config for fuzzy system {FuzzySystemId}", fuzzySystemId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<WeatherAlertConfigDto> UpdateAlertConfigAsync(
        string fuzzySystemId, UpdateAlertConfigRequestDto request, CancellationToken cancellationToken = default)
    {
        try
        {
            var url = $"{_baseUrl}/api/weather/alerts/config/{Uri.EscapeDataString(fuzzySystemId)}";
            var json = JsonSerializer.Serialize(request, _jsonOptions);
            var httpContent = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PutAsync(url, httpContent, cancellationToken);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<WeatherAlertConfigDto>(content, _jsonOptions)
                ?? throw new InvalidOperationException("Failed to deserialize updated alert config");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating alert config for fuzzy system {FuzzySystemId}", fuzzySystemId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<WeatherAlertConfigDto> SeedAlertConfigAsync(
        string fuzzySystemId, SeedAlertConfigRequestDto request, CancellationToken cancellationToken = default)
    {
        try
        {
            var url = $"{_baseUrl}/api/weather/alerts/config/{Uri.EscapeDataString(fuzzySystemId)}/seed";
            var json = JsonSerializer.Serialize(request, _jsonOptions);
            var httpContent = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(url, httpContent, cancellationToken);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<WeatherAlertConfigDto>(content, _jsonOptions)
                ?? throw new InvalidOperationException("Failed to deserialize seeded alert config");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error seeding alert config for fuzzy system {FuzzySystemId}", fuzzySystemId);
            throw;
        }
    }

    // --- Alert history endpoints ---

    /// <inheritdoc />
    public async Task<List<WeatherAlertDto>> GetAlertsAsync(
        string? fuzzySystemId = null, string? userId = null,
        DateTime? from = null, DateTime? to = null,
        bool? unreadOnly = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var queryParams = new List<string>();
            if (!string.IsNullOrEmpty(fuzzySystemId)) queryParams.Add($"fuzzySystemId={Uri.EscapeDataString(fuzzySystemId)}");
            if (!string.IsNullOrEmpty(userId)) queryParams.Add($"userId={Uri.EscapeDataString(userId)}");
            if (from.HasValue) queryParams.Add($"from={from.Value:O}");
            if (to.HasValue) queryParams.Add($"to={to.Value:O}");
            if (unreadOnly == true) queryParams.Add("unreadOnly=true");

            var qs = queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : "";
            var url = $"{_baseUrl}/api/weather/alerts{qs}";

            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<List<WeatherAlertDto>>(content, _jsonOptions) ?? [];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching weather alerts");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task MarkAlertReadAsync(string alertId, string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var url = $"{_baseUrl}/api/weather/alerts/{Uri.EscapeDataString(alertId)}/read?userId={Uri.EscapeDataString(userId)}";
            var response = await _httpClient.PatchAsync(url, null, cancellationToken);
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking alert {AlertId} as read for user {UserId}", alertId, userId);
            throw;
        }
    }
}
