using BffService.Domain.DTOs;
using HydroEspinaca.Shared.DTOs.Analytics;

namespace BffService.Application.Interfaces;

/// <summary>
/// Contract for the analytics service that aggregates data from multiple backend services.
/// </summary>
public interface IAnalyticsService
{
    Task<EnvironmentalAggregatesResponse> GetEnvironmentalAggregatesAsync(
        EnvironmentalAnalyticsRequest request,
        string? accessToken,
        CancellationToken cancellationToken = default);

    Task<ActuatorAnalyticsResponse> GetActuatorAnalyticsAsync(
        ActuatorAnalyticsRequest request,
        string? accessToken,
        CancellationToken cancellationToken = default);
}
