using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;
using WeatherService.Domain.DTOs;
using WeatherService.Domain.Interfaces;
using WeatherService.Domain.Settings;
using WeatherService.Infrastructure.Clients.Models;

namespace WeatherService.Infrastructure.Clients;

/// <summary>
/// OpenWeather One Call API 3.0 client with in-memory caching.
/// A single /onecall request returns current + minutely + hourly (48h) + daily (8 days) + government alerts.
/// Cache TTL: configurable (default 30 minutes), yielding ~48 API calls/day (well within 1,000/day free).
/// </summary>
public class OpenWeatherClient : IOpenWeatherClient
{
    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly ILogger<OpenWeatherClient> _logger;
    private readonly OpenWeatherSettings _openWeatherSettings;
    private readonly WeatherSettings _weatherSettings;
    private readonly JsonSerializerOptions _jsonOptions;

    private const string ForecastCacheKey = "onecall_forecast";

    public OpenWeatherClient(
        HttpClient httpClient,
        IMemoryCache cache,
        IOptions<OpenWeatherSettings> openWeatherSettings,
        IOptions<WeatherSettings> weatherSettings,
        ILogger<OpenWeatherClient> logger)
    {
        _httpClient = httpClient;
        _cache = cache;
        _logger = logger;
        _openWeatherSettings = openWeatherSettings.Value;
        _weatherSettings = weatherSettings.Value;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    /// <inheritdoc />
    public async Task<ForecastResponseDto> GetForecastAsync(CancellationToken cancellationToken = default)
    {
        // Try to get the forecast from cache first
        if (_cache.TryGetValue(ForecastCacheKey, out ForecastResponseDto? cached) && cached != null)
        {
            _logger.LogDebug("Forecast data retrieved from cache (fetched at {FetchedAt})", cached.FetchedAt);
            return cached;
        }

        // Cache miss - fetch from OpenWeather API
        var forecast = await FetchOneCallAsync(cancellationToken);

        // Cache the forecast with an absolute expiration based on settings
        var cacheMinutes = _weatherSettings.ForecastCacheMinutes;
        _cache.Set(ForecastCacheKey, forecast, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(cacheMinutes)
        });

        _logger.LogInformation("Forecast data fetched and cached for {Minutes} minutes", cacheMinutes);
        return forecast;
    }

    /// <inheritdoc />
    public async Task<CurrentWeatherDto> GetCurrentWeatherAsync(CancellationToken cancellationToken = default)
    {
        var forecast = await GetForecastAsync(cancellationToken);
        return forecast.Current;
    }

    /// <inheritdoc />
    public async Task<List<DailyForecastDto>> GetDailyForecastAsync(CancellationToken cancellationToken = default)
    {
        var forecast = await GetForecastAsync(cancellationToken);
        return forecast.Daily;
    }

    /// <summary>
    /// Calls OpenWeather One Call API 3.0 and maps the response to DTOs
    /// </summary>
    private async Task<ForecastResponseDto> FetchOneCallAsync(CancellationToken cancellationToken)
    {
        // Construct the API URL with query parameters
        var url = $"{_openWeatherSettings.BaseUrl}/onecall" +
                  $"?lat={_openWeatherSettings.Latitude}" +
                  $"&lon={_openWeatherSettings.Longitude}" +
                  $"&units=metric&lang=es" +
                  $"&appid={_openWeatherSettings.ApiKey}";

        _logger.LogInformation("Calling OpenWeather One Call 3.0 API");
        _logger.LogDebug("URL: {Url}", url.Replace(_openWeatherSettings.ApiKey, "***"));

        try
        {
            // Make the HTTP GET request to the OpenWeather API
            var response = await _httpClient.GetAsync(url, cancellationToken);

            // Check if the response indicates success; if not, log the error and try to return cached data as fallback
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("OpenWeather API returned {StatusCode}: {Error}", response.StatusCode, errorContent);

                // Try returning cached data as fallback even if expired
                if (_cache.TryGetValue(ForecastCacheKey, out ForecastResponseDto? fallback) && fallback != null)
                {
                    _logger.LogWarning("Returning expired cached forecast as fallback");
                    return fallback;
                }

                response.EnsureSuccessStatusCode();
            }

            // Deserialize the response content into the OneCallResponse model and map it to ForecastResponseDto
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var data = JsonSerializer.Deserialize<OneCallResponse>(content, _jsonOptions);

            if (data == null)
            {
                throw new InvalidOperationException("Failed to deserialize One Call 3.0 response");
            }

            return MapToForecastResponse(data);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error fetching One Call 3.0 data");

            if (_cache.TryGetValue(ForecastCacheKey, out ForecastResponseDto? fallback) && fallback != null)
            {
                _logger.LogWarning("Returning expired cached forecast as fallback after HTTP error");
                return fallback;
            }

            throw;
        }
    }

    /// <summary>
    /// Maps the OneCallResponse model from OpenWeather API to the ForecastResponseDto used in our application. 
    /// This involves converting units, rounding values, and handling any missing data gracefully. 
    /// The mapping ensures that the DTO contains all the necessary information for current weather, 
    /// hourly forecasts, daily forecasts, and government alerts in a format 
    /// suitable for our application's needs.
    /// </summary>
    /// <param name="data">The OneCallResponse model from OpenWeather API</param>
    /// <returns>A ForecastResponseDto mapped from the input data</returns>
    private ForecastResponseDto MapToForecastResponse(OneCallResponse data)
    {
        var result = new ForecastResponseDto
        {
            FetchedAt = DateTime.UtcNow,
            Timezone = data.Timezone,
            TimezoneOffset = data.TimezoneOffset
        };

        // Map current weather
        if (data.Current != null)
        {
            result.Current = MapCurrentWeather(data.Current);
        }

        // Map hourly (48 data points)
        if (data.Hourly != null)
        {
            result.Hourly = data.Hourly.Select(MapHourlyForecast).ToList();
        }

        // Map daily (8 data points)
        if (data.Daily != null)
        {
            result.Daily = data.Daily.Select(MapDailyForecast).ToList();
        }

        // Map government alerts
        if (data.Alerts != null)
        {
            result.GovernmentAlerts = data.Alerts.Select(MapGovernmentAlert).ToList();
        }

        return result;
    }

    private static CurrentWeatherDto MapCurrentWeather(CurrentData current)
    {
        var weather = current.Weather.FirstOrDefault();

        return new CurrentWeatherDto
        {
            Temperature = Math.Round(current.Temp, 1),
            FeelsLike = Math.Round(current.FeelsLike, 1),
            Humidity = current.Humidity,
            Pressure = current.Pressure,
            DewPoint = Math.Round(current.DewPoint, 1),
            Uvi = current.Uvi,
            Cloudiness = current.Clouds,
            Visibility = current.Visibility,
            WindSpeed = Math.Round(current.WindSpeed, 1),
            WindGust = current.WindGust.HasValue ? Math.Round(current.WindGust.Value, 1) : null,
            WindDeg = current.WindDeg,
            Main = weather?.Main ?? "Unknown",
            Description = weather?.Description ?? "No disponible",
            Icon = weather?.Icon ?? "01d",
            WeatherId = weather?.Id ?? 0,
            Rain1h = current.Rain?.OneHour,
            Snow1h = current.Snow?.OneHour,
            Sunrise = DateTimeOffset.FromUnixTimeSeconds(current.Sunrise).UtcDateTime,
            Sunset = DateTimeOffset.FromUnixTimeSeconds(current.Sunset).UtcDateTime,
            LastUpdate = DateTimeOffset.FromUnixTimeSeconds(current.Dt).UtcDateTime
        };
    }

    private static HourlyForecastDto MapHourlyForecast(HourlyData hourly)
    {
        var weather = hourly.Weather.FirstOrDefault();

        return new HourlyForecastDto
        {
            DateTime = DateTimeOffset.FromUnixTimeSeconds(hourly.Dt).UtcDateTime,
            Temperature = Math.Round(hourly.Temp, 1),
            FeelsLike = Math.Round(hourly.FeelsLike, 1),
            Pressure = hourly.Pressure,
            Humidity = hourly.Humidity,
            DewPoint = Math.Round(hourly.DewPoint, 1),
            Uvi = hourly.Uvi,
            Cloudiness = hourly.Clouds,
            Visibility = hourly.Visibility,
            WindSpeed = Math.Round(hourly.WindSpeed, 1),
            WindGust = hourly.WindGust.HasValue ? Math.Round(hourly.WindGust.Value, 1) : null,
            WindDeg = hourly.WindDeg,
            Pop = Math.Round(hourly.Pop, 2),
            Rain1h = hourly.Rain?.OneHour,
            Snow1h = hourly.Snow?.OneHour,
            Main = weather?.Main ?? "Unknown",
            Description = weather?.Description ?? "No disponible",
            Icon = weather?.Icon ?? "01d",
            WeatherId = weather?.Id ?? 0
        };
    }

    private static DailyForecastDto MapDailyForecast(DailyData daily)
    {
        var weather = daily.Weather.FirstOrDefault();

        return new DailyForecastDto
        {
            DateTime = DateTimeOffset.FromUnixTimeSeconds(daily.Dt).UtcDateTime,
            Sunrise = DateTimeOffset.FromUnixTimeSeconds(daily.Sunrise).UtcDateTime,
            Sunset = DateTimeOffset.FromUnixTimeSeconds(daily.Sunset).UtcDateTime,
            Moonrise = DateTimeOffset.FromUnixTimeSeconds(daily.Moonrise).UtcDateTime,
            Moonset = DateTimeOffset.FromUnixTimeSeconds(daily.Moonset).UtcDateTime,
            MoonPhase = daily.MoonPhase,
            TempMin = Math.Round(daily.Temp.Min, 1),
            TempMax = Math.Round(daily.Temp.Max, 1),
            TempMorn = Math.Round(daily.Temp.Morn, 1),
            TempDay = Math.Round(daily.Temp.Day, 1),
            TempEve = Math.Round(daily.Temp.Eve, 1),
            TempNight = Math.Round(daily.Temp.Night, 1),
            FeelsLikeMorn = Math.Round(daily.FeelsLike.Morn, 1),
            FeelsLikeDay = Math.Round(daily.FeelsLike.Day, 1),
            FeelsLikeEve = Math.Round(daily.FeelsLike.Eve, 1),
            FeelsLikeNight = Math.Round(daily.FeelsLike.Night, 1),
            Humidity = daily.Humidity,
            Pressure = daily.Pressure,
            DewPoint = Math.Round(daily.DewPoint, 1),
            Cloudiness = daily.Clouds,
            WindSpeed = Math.Round(daily.WindSpeed, 1),
            WindGust = daily.WindGust.HasValue ? Math.Round(daily.WindGust.Value, 1) : null,
            WindDeg = daily.WindDeg,
            Uvi = daily.Uvi,
            Pop = Math.Round(daily.Pop, 2),
            Rain = daily.Rain,
            Snow = daily.Snow,
            Main = weather?.Main ?? "Unknown",
            Description = weather?.Description ?? "No disponible",
            Icon = weather?.Icon ?? "01d",
            WeatherId = weather?.Id ?? 0,
            Summary = daily.Summary
        };
    }

    private static GovernmentAlertDto MapGovernmentAlert(AlertData alert)
    {
        return new GovernmentAlertDto
        {
            SenderName = alert.SenderName,
            Event = alert.Event,
            Start = DateTimeOffset.FromUnixTimeSeconds(alert.Start).UtcDateTime,
            End = DateTimeOffset.FromUnixTimeSeconds(alert.End).UtcDateTime,
            Description = alert.Description,
            Tags = alert.Tags
        };
    }
}
