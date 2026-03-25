using MediatR;
using WeatherService.Domain.Entities;

namespace WeatherService.Application.Features.Alerts.Queries.GetAlerts;

/// <summary>
/// Gets weather alerts with optional filters.
/// </summary>
public record GetAlertsQuery(
    string? FuzzySystemId = null,
    string? UserId = null,
    DateTime? From = null,
    DateTime? To = null,
    bool? UnreadOnly = null) : IRequest<List<WeatherAlert>>;
