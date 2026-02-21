using MediatR;
using WeatherService.Domain.DTOs;
using WeatherService.Domain.Interfaces;

namespace WeatherService.Application.Features.Weather.Queries.GetDailyForecast;

/// <summary>
/// Handler for GetDailyForecastQuery. Returns only the daily[] block from One Call 3.0.
/// </summary>
public class GetDailyForecastQueryHandler : IRequestHandler<GetDailyForecastQuery, List<DailyForecastDto>>
{
    private readonly IOpenWeatherClient _weatherClient;

    public GetDailyForecastQueryHandler(IOpenWeatherClient weatherClient)
    {
        _weatherClient = weatherClient;
    }

    public async Task<List<DailyForecastDto>> Handle(GetDailyForecastQuery request, CancellationToken cancellationToken)
    {
        return await _weatherClient.GetDailyForecastAsync(cancellationToken);
    }
}
