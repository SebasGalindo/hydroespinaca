using WeatherService.Domain.Settings;

namespace WeatherService.Domain.Tests.Settings;

public class SettingsTests
{
    [Fact]
    public void OpenWeatherSettings_HasDefaultValues()
    {
        var settings = new OpenWeatherSettings();

        settings.BaseUrl.Should().Be("https://api.openweathermap.org/data/3.0");
        settings.ApiKey.Should().BeEmpty();
        settings.Latitude.Should().BeApproximately(4.7002001, 0.001);
        settings.Longitude.Should().BeApproximately(-74.2385058, 0.001);
    }

    [Fact]
    public void WeatherSettings_HasDefaultValues()
    {
        var settings = new WeatherSettings();

        settings.ForecastCacheMinutes.Should().Be(30);
        settings.AlertEvaluationIntervalMinutes.Should().Be(30);
        settings.AlertDeduplicationHours.Should().Be(6);
        settings.TimezoneOffsetSeconds.Should().Be(-18000);
    }

    [Fact]
    public void OpenWeatherSettings_PropertiesCanBeSet()
    {
        var settings = new OpenWeatherSettings
        {
            BaseUrl = "https://custom.api.com",
            ApiKey = "test-key",
            Latitude = 10.0,
            Longitude = -75.0
        };

        settings.BaseUrl.Should().Be("https://custom.api.com");
        settings.ApiKey.Should().Be("test-key");
        settings.Latitude.Should().Be(10.0);
        settings.Longitude.Should().Be(-75.0);
    }
}
