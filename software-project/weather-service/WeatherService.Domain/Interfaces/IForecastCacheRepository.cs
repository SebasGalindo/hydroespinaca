using WeatherService.Domain.Entities;

namespace WeatherService.Domain.Interfaces;

/// <summary>
/// Repository interface for managing forecast cache entries.
/// This repository abstracts the data access layer for storing and retrieving
/// forecast data fetched from the external API,
/// allowing for caching and efficient retrieval.
/// </summary>
public interface IForecastCacheRepository
{
    /// <summary>
    /// Retrieves the latest forecast cache entry from the data store.
    /// Returns null if no cache entry exists or if the cache is expired.
    /// </summary>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>The latest forecast cache entry, or null if not found or expired.</returns
    Task<ForecastCache?> GetLatestAsync(CancellationToken ct = default);

    /// <summary>
    /// Inserts or updates a forecast cache entry in the data store.
    /// If an entry with the same location and timestamp already exists, 
    /// it will be updated;
    /// </summary>
    /// <param name="cache">The forecast cache entry to insert or update.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    Task UpsertAsync(ForecastCache cache, CancellationToken ct = default);
}
