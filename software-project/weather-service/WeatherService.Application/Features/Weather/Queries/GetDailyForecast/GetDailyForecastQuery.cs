using MediatR;
using WeatherService.Domain.DTOs;

namespace WeatherService.Application.Features.Weather.Queries.GetDailyForecast;

/// <summary>
/// Query to get only the daily forecast block (8 days: today + 7)
/// </summary>
public record GetDailyForecastQuery : IRequest<List<DailyForecastDto>>;
