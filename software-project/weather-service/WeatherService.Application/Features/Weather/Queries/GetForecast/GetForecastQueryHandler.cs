using MediatR;
using WeatherService.Domain.DTOs;
using WeatherService.Domain.Interfaces;

namespace WeatherService.Application.Features.Weather.Queries.GetForecast;

/// <summary>
/// Handler for GetForecastQuery. Returns the full One Call 3.0 forecast response.
/// </summary>
public class GetForecastQueryHandler : IRequestHandler<GetForecastQuery, ForecastResponseDto>
{
    private readonly IOpenWeatherClient _weatherClient;

    public GetForecastQueryHandler(IOpenWeatherClient weatherClient)
    {
        _weatherClient = weatherClient;
    }

    public async Task<ForecastResponseDto> Handle(GetForecastQuery request, CancellationToken cancellationToken)
    {
        return await _weatherClient.GetForecastAsync(cancellationToken);
    }
}
