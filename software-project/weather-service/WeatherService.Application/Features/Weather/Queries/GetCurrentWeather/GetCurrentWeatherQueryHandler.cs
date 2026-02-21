using MediatR;
using WeatherService.Domain.DTOs;
using WeatherService.Domain.Interfaces;

namespace WeatherService.Application.Features.Weather.Queries.GetCurrentWeather;

/// <summary>
/// Handler for GetCurrentWeatherQuery. Returns current weather from cached One Call 3.0 data.
/// </summary>
public class GetCurrentWeatherQueryHandler : IRequestHandler<GetCurrentWeatherQuery, CurrentWeatherDto>
{
    private readonly IOpenWeatherClient _weatherClient;

    public GetCurrentWeatherQueryHandler(IOpenWeatherClient weatherClient)
    {
        _weatherClient = weatherClient;
    }

    public async Task<CurrentWeatherDto> Handle(GetCurrentWeatherQuery request, CancellationToken cancellationToken)
    {
        return await _weatherClient.GetCurrentWeatherAsync(cancellationToken);
    }
}
