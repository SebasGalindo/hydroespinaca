using NotificationService.Domain.Entities;

namespace NotificationService.Domain.Interfaces;

/// <summary>
/// Aggregates data from multiple microservices for the daily summary notification.
/// </summary>
public interface IDailySummaryDataAggregator
{
    /// <summary>
    /// Gathers sensor averages, actuator runtime, fuzzy evaluations and weather forecast
    /// for the specified user's daily summary. Only fetches data for sections the user has enabled.
    /// </summary>
    /// <param name="userId">The ID of the user to aggregate data for.</param>
    /// <param name="preference">The user's notification preferences (to know which sections to include).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A DailySummaryData object containing all the aggregated information for the summary.</returns>
    Task<DailySummaryData> AggregateAsync(
        string userId,
        NotificationPreference preference,
        CancellationToken ct = default);
}
