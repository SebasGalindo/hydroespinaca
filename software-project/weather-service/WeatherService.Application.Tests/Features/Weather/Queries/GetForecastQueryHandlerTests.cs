using WeatherService.Application.Features.Weather.Queries.GetForecast;
using WeatherService.Domain.DTOs;
using WeatherService.Domain.Interfaces;

namespace WeatherService.Application.Tests.Features.Weather.Queries;

public class GetForecastQueryHandlerTests
{
    private readonly Mock<IOpenWeatherClient> _weatherClientMock = new();
    private readonly GetForecastQueryHandler _handler;

    public GetForecastQueryHandlerTests()
    {
        _handler = new GetForecastQueryHandler(_weatherClientMock.Object);
    }

    [Fact]
    public async Task Handle_ReturnsFullForecast()
    {
        var now = DateTime.UtcNow;
        var response = new ForecastResponseDto
        {
            Current = new CurrentWeatherDto { Temperature = 25 },
            Hourly = new List<HourlyForecastDto> { new() { Temperature = 24 } },
            Daily = new List<DailyForecastDto> { new() { TempMax = 30 } },
            GovernmentAlerts = new List<GovernmentAlertDto>(),
            FetchedAt = now,
            Timezone = "America/Bogota",
            TimezoneOffset = -18000
        };

        _weatherClientMock
            .Setup(c => c.GetForecastAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var result = await _handler.Handle(new GetForecastQuery(), CancellationToken.None);

        result.Current.Temperature.Should().Be(25);
        result.Hourly.Should().HaveCount(1);
        result.Daily.Should().HaveCount(1);
        result.Timezone.Should().Be("America/Bogota");
    }
}
