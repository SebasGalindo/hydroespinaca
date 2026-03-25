using MediatR;
using WeatherService.Domain.DTOs;

namespace WeatherService.Application.Features.Weather.Queries.GetForecast;

/// <summary>
/// Query to get full forecast: current + hourly 48h + daily 8 days + government alerts
/// </summary>
public record GetForecastQuery : IRequest<ForecastResponseDto>;
