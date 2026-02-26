using WeatherService.Application.Features.Weather.Queries.GetDailyForecast;
using WeatherService.Domain.DTOs;
using WeatherService.Domain.Interfaces;

namespace WeatherService.Application.Tests.Features.Weather.Queries;

public class GetDailyForecastQueryHandlerTests
{
    private readonly Mock<IOpenWeatherClient> _weatherClientMock = new();
    private readonly GetDailyForecastQueryHandler _handler;

    public GetDailyForecastQueryHandlerTests()
    {
        _handler = new GetDailyForecastQueryHandler(_weatherClientMock.Object);
    }

    [Fact]
    public async Task Handle_ReturnsDailyForecast()
    {
        var forecast = new List<DailyForecastDto>
        {
            new() { TempMax = 32, TempMin = 20, Summary = "Sunny" },
            new() { TempMax = 28, TempMin = 18, Summary = "Cloudy" }
        };

        _weatherClientMock
            .Setup(c => c.GetDailyForecastAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(forecast);

        var result = await _handler.Handle(new GetDailyForecastQuery(), CancellationToken.None);

        result.Should().HaveCount(2);
        result[0].TempMax.Should().Be(32);
    }

    [Fact]
    public async Task Handle_EmptyForecast_ReturnsEmptyList()
    {
        _weatherClientMock
            .Setup(c => c.GetDailyForecastAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DailyForecastDto>());

        var result = await _handler.Handle(new GetDailyForecastQuery(), CancellationToken.None);

        result.Should().BeEmpty();
    }
}
