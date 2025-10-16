using BffService.Application.Interfaces;
using BffService.Domain.DTOs;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BffService.Infrastructure.Services;

/// <summary>
/// Service for fetching weather data from OpenWeather API v2.5 (free tier)
/// Implements caching with hourly refresh at exact hours (00:00, 01:00, etc.)
/// </summary>
public class WeatherService : IWeatherService
{
    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly ILogger<WeatherService> _logger;
    private readonly string _baseUrl;
    private readonly double _latitude;
    private readonly double _longitude;
    private readonly string _apiKey;
    private readonly int _timezoneOffsetSeconds;
    private readonly JsonSerializerOptions _jsonOptions;

    private const string CacheKey = "weather_data";

    public WeatherService(
        HttpClient httpClient,
        IMemoryCache cache,
        IConfiguration configuration,
        ILogger<WeatherService> logger)
    {
        _httpClient = httpClient;
        _cache = cache;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        // Read configuration
        _baseUrl = configuration["ExternalApis:OpenWeather:BaseUrl"]
            ?? throw new InvalidOperationException("OpenWeather BaseUrl not configured");
        _latitude = double.Parse(configuration["ExternalApis:OpenWeather:Latitude"] ?? "4.7059");
        _longitude = double.Parse(configuration["ExternalApis:OpenWeather:Longitude"] ?? "-74.2302");
        _apiKey = configuration["ExternalApis:OpenWeather:ApiKey"]
            ?? throw new InvalidOperationException("OpenWeather ApiKey not configured");

        // Colombia (Bogotá) is UTC-5 = -18000 seconds
        _timezoneOffsetSeconds = -18000;
    }

    public async Task<WeatherDto> GetWeatherAsync(CancellationToken cancellationToken = default)
    {
        // Try to get from cache first and check if still valid
        if (_cache.TryGetValue(CacheKey, out WeatherDto? cachedWeather) && cachedWeather != null)
        {
            // Check if we're still in the same hour
            if (IsCacheStillValid(cachedWeather.LastUpdate))
            {
                _logger.LogDebug("Weather data retrieved from cache");
                return cachedWeather;
            }
        }

        try
        {
            _logger.LogInformation("Fetching weather data from OpenWeather API");

            // Build request URL for FREE 2.5/weather endpoint
            var url = $"{_baseUrl}?lat={_latitude}&lon={_longitude}&units=metric&lang=es&appid={_apiKey}";

            _logger.LogDebug("OpenWeather API URL: {Url}", url.Replace(_apiKey, "***"));

            var response = await _httpClient.GetAsync(url, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("OpenWeather API returned status {StatusCode}: {ErrorContent}",
                    response.StatusCode, errorContent);
                response.EnsureSuccessStatusCode();
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogDebug("OpenWeather API response: {Content}", content);

            var weatherData = JsonSerializer.Deserialize<OpenWeatherResponse>(content, _jsonOptions);

            if (weatherData == null)
            {
                throw new InvalidOperationException("Failed to deserialize weather data");
            }

            // Map to DTO
            var weatherDto = MapToDto(weatherData);

            // Cache until next hour
            var cacheExpiration = CalculateNextHourExpiration();
            _cache.Set(CacheKey, weatherDto, new MemoryCacheEntryOptions
            {
                AbsoluteExpiration = cacheExpiration
            });

            _logger.LogInformation("Weather data fetched and cached successfully until {Expiration}", cacheExpiration);
            return weatherDto;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error fetching weather data");

            // If cached data exists (even expired), return it as fallback
            if (_cache.TryGetValue(CacheKey, out WeatherDto? fallbackWeather) && fallbackWeather != null)
            {
                _logger.LogWarning("Returning expired cached weather data as fallback");
                return fallbackWeather;
            }

            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error fetching weather data");
            throw;
        }
    }

    /// <summary>
    /// Checks if cached data is still valid (within the same hour)
    /// </summary>
    private bool IsCacheStillValid(DateTime cacheTime)
    {
        var now = GetLocalTime();
        // Cache is valid if we're in the same hour
        return cacheTime.Year == now.Year &&
               cacheTime.Month == now.Month &&
               cacheTime.Day == now.Day &&
               cacheTime.Hour == now.Hour;
    }

    /// <summary>
    /// Calculates when the cache should expire (start of next hour)
    /// </summary>
    private DateTimeOffset CalculateNextHourExpiration()
    {
        var now = GetLocalTime();
        var nextHour = new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0).AddHours(1);
        return new DateTimeOffset(nextHour, TimeSpan.FromSeconds(_timezoneOffsetSeconds));
    }

    /// <summary>
    /// Gets current time in local timezone (Colombia UTC-5)
    /// </summary>
    private DateTime GetLocalTime()
    {
        return DateTime.UtcNow.AddSeconds(_timezoneOffsetSeconds);
    }

    /// <summary>
    /// Converts Unix timestamp to local DateTime
    /// </summary>
    private DateTime UnixTimestampToLocalDateTime(long timestamp)
    {
        return DateTimeOffset.FromUnixTimeSeconds(timestamp)
            .AddSeconds(_timezoneOffsetSeconds)
            .DateTime;
    }

    private WeatherDto MapToDto(OpenWeatherResponse data)
    {
        return new WeatherDto
        {
            Temperature = Math.Round(data.Main.Temp, 1),
            FeelsLike = Math.Round(data.Main.FeelsLike, 1),
            Humidity = data.Main.Humidity,
            Main = data.Weather.FirstOrDefault()?.Main ?? "Unknown",
            Description = data.Weather.FirstOrDefault()?.Description ?? "No disponible",
            Icon = data.Weather.FirstOrDefault()?.Icon ?? "01d",
            WindSpeed = Math.Round(data.Wind.Speed, 1),
            Cloudiness = data.Clouds.All,
            Rain1h = data.Rain?.OneHour,
            Sunrise = UnixTimestampToLocalDateTime(data.Sys.Sunrise),
            Sunset = UnixTimestampToLocalDateTime(data.Sys.Sunset),
            LastUpdate = UnixTimestampToLocalDateTime(data.Dt)
        };
    }

    #region OpenWeather API Response Models (2.5/weather endpoint)

    private class OpenWeatherResponse
    {
        public required MainWeatherData Main { get; set; }
        public required List<WeatherCondition> Weather { get; set; }
        public required WindData Wind { get; set; }
        public required CloudsData Clouds { get; set; }
        public RainData? Rain { get; set; }
        public required SysData Sys { get; set; }

        /// <summary>
        /// Time of data calculation, unix UTC
        /// </summary>
        public long Dt { get; set; }

        /// <summary>
        /// Shift in seconds from UTC
        /// </summary>
        public int Timezone { get; set; }
    }

    private class MainWeatherData
    {
        public double Temp { get; set; }

        [JsonPropertyName("feels_like")]
        public double FeelsLike { get; set; }

        public int Humidity { get; set; }
    }

    private class WindData
    {
        public double Speed { get; set; }
    }

    private class CloudsData
    {
        /// <summary>
        /// Cloudiness percentage
        /// </summary>
        public int All { get; set; }
    }

    private class RainData
    {
        /// <summary>
        /// Rain volume for last hour in mm
        /// </summary>
        [JsonPropertyName("1h")]
        public double OneHour { get; set; }
    }

    private class SysData
    {
        /// <summary>
        /// Sunrise time, unix UTC
        /// </summary>
        public long Sunrise { get; set; }

        /// <summary>
        /// Sunset time, unix UTC
        /// </summary>
        public long Sunset { get; set; }
    }

    private class WeatherCondition
    {
        public string Main { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
    }

    #endregion
}
