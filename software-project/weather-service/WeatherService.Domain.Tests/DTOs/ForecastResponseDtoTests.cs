using WeatherService.Domain.DTOs;

namespace WeatherService.Domain.Tests.DTOs;

public class ForecastResponseDtoTests
{
    [Fact]
    public void Defaults_InitializeCollections()
    {
        var dto = new ForecastResponseDto();

        dto.Current.Should().NotBeNull();
        dto.Hourly.Should().NotBeNull().And.BeEmpty();
        dto.Daily.Should().NotBeNull().And.BeEmpty();
        dto.GovernmentAlerts.Should().NotBeNull().And.BeEmpty();
        dto.Timezone.Should().BeEmpty();
        dto.TimezoneOffset.Should().Be(0);
    }

    [Fact]
    public void AllProperties_CanBeSet()
    {
        var now = DateTime.UtcNow;
        var dto = new ForecastResponseDto
        {
            Current = new CurrentWeatherDto { Temperature = 25 },
            Hourly = new List<HourlyForecastDto> { new() { Temperature = 24 } },
            Daily = new List<DailyForecastDto> { new() { TempMax = 30 } },
            GovernmentAlerts = new List<GovernmentAlertDto> { new() { Event = "Heat Wave" } },
            FetchedAt = now,
            Timezone = "America/Bogota",
            TimezoneOffset = -18000
        };

        dto.Current.Temperature.Should().Be(25);
        dto.Hourly.Should().HaveCount(1);
        dto.Daily.Should().HaveCount(1);
        dto.GovernmentAlerts.Should().HaveCount(1);
        dto.Timezone.Should().Be("America/Bogota");
        dto.TimezoneOffset.Should().Be(-18000);
    }
}
