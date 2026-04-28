using MediatR;
using WeatherService.Domain.Entities;

namespace WeatherService.Application.Features.Alerts.Commands.UpdateAlertConfig;

/// <summary>
/// Updates the alert configuration for a fuzzy system.
/// </summary>
public record UpdateAlertConfigCommand(
    string FuzzySystemId,
    string UserId,
    bool IsActive,
    List<AlertThresholdDto> Alerts,
    int MaxForecastDays = 8,
    bool AllowDuplicateAlerts = true) : IRequest<WeatherAlertConfig>;

public record AlertThresholdDto(
    string Type,
    bool Enabled,
    double? ThresholdValue,
    string? Comparison,
    string Recommendation);
