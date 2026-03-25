using WeatherService.Application.Features.Weather.Queries.GetCurrentWeather;
using WeatherService.Domain.DTOs;
using WeatherService.Domain.Interfaces;

namespace WeatherService.Application.Tests.Features.Weather.Queries;

public class GetCurrentWeatherQueryHandlerTests
{
    private readonly Mock<IOpenWeatherClient> _weatherClientMock = new();
    private readonly GetCurrentWeatherQueryHandler _handler;

    public GetCurrentWeatherQueryHandlerTests()
    {
        _handler = new GetCurrentWeatherQueryHandler(_weatherClientMock.Object);
    }

    [Fact]
    public async Task Handle_ReturnsCurrentWeather()
    {
        var weather = new CurrentWeatherDto
        {
            Temperature = 28.5,
            Humidity = 75,
            Main = "Clouds"
        };

        _weatherClientMock
            .Setup(c => c.GetCurrentWeatherAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(weather);

        var result = await _handler.Handle(new GetCurrentWeatherQuery(), CancellationToken.None);

        result.Temperature.Should().Be(28.5);
        result.Humidity.Should().Be(75);
        result.Main.Should().Be("Clouds");
    }
}
