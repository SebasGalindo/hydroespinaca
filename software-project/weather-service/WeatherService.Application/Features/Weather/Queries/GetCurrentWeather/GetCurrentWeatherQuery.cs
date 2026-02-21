using MediatR;
using WeatherService.Domain.DTOs;

namespace WeatherService.Application.Features.Weather.Queries.GetCurrentWeather;

/// <summary>
/// Query to get current weather conditions from One Call API 3.0
/// </summary>
public record GetCurrentWeatherQuery : IRequest<CurrentWeatherDto>;
