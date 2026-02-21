using MediatR;
using WeatherService.Domain.Entities;

namespace WeatherService.Application.Features.Alerts.Commands.SeedAlertConfig;

/// <summary>
/// Creates a default alert config with predefined thresholds for a fuzzy system.
/// </summary>
public record SeedAlertConfigCommand(
    string FuzzySystemId,
    string FuzzySystemName,
    string UserId) : IRequest<WeatherAlertConfig>;
