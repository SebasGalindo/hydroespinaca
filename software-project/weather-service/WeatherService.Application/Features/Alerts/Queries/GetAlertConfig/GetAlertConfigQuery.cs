using MediatR;
using WeatherService.Domain.Entities;

namespace WeatherService.Application.Features.Alerts.Queries.GetAlertConfig;

/// <summary>
/// Gets the alert config for a specific fuzzy system.
/// </summary>
public record GetAlertConfigQuery(string FuzzySystemId) : IRequest<WeatherAlertConfig?>;
