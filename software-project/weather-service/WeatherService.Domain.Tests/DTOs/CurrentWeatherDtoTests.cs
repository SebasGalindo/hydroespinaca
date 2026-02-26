using WeatherService.Domain.DTOs;

namespace WeatherService.Domain.Tests.DTOs;

public class CurrentWeatherDtoTests
{
    [Fact]
    public void Defaults_AreReasonable()
    {
        var dto = new CurrentWeatherDto();

        dto.Temperature.Should().Be(0);
        dto.Humidity.Should().Be(0);
        dto.Main.Should().BeEmpty();
        dto.Description.Should().BeEmpty();
        dto.Icon.Should().BeEmpty();
        dto.Rain1h.Should().BeNull();
        dto.Snow1h.Should().BeNull();
        dto.WindGust.Should().BeNull();
    }

    [Fact]
    public void AllProperties_CanBeSet()
    {
        var now = DateTime.UtcNow;
        var dto = new CurrentWeatherDto
        {
            Temperature = 28.5,
            FeelsLike = 30.1,
            Humidity = 75,
            Pressure = 1013,
            DewPoint = 22.3,
            Uvi = 6.5,
            Cloudiness = 40,
            Visibility = 10000,
            WindSpeed = 3.5,
            WindGust = 7.2,
            WindDeg = 180,
            Main = "Clouds",
            Description = "scattered clouds",
            Icon = "03d",
            WeatherId = 802,
            Rain1h = 0.5,
            Snow1h = null,
            Sunrise = now.AddHours(-6),
            Sunset = now.AddHours(6),
            LastUpdate = now
        };

        dto.Temperature.Should().Be(28.5);
        dto.FeelsLike.Should().Be(30.1);
        dto.Humidity.Should().Be(75);
        dto.WindGust.Should().Be(7.2);
        dto.Rain1h.Should().Be(0.5);
        dto.WeatherId.Should().Be(802);
    }
}
