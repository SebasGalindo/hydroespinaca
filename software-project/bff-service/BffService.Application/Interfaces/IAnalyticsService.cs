using BffService.Domain.DTOs;

namespace BffService.Application.Interfaces;

public interface IAnalyticsService
{
    Task<EnvironmentalAggregatesResponse> GetEnvironmentalAggregatesAsync(
        GetEnvironmentalAggregatesRequest request,
        string? accessToken,
        CancellationToken cancellationToken = default);
}
