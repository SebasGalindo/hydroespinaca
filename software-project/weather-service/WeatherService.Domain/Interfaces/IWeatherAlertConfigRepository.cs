using WeatherService.Domain.Entities;

namespace WeatherService.Domain.Interfaces;

/// <summary>
/// Repository interface for managing WeatherAlertConfig entities.
/// This repository abstracts the data access layer for storing and retrieving
/// user-defined alert configurations, allowing for CRUD operations and
/// fuzzy matching by system ID to associate alerts with specific weather stations or locations.
/// </summary>
public interface IWeatherAlertConfigRepository
{
    /// <summary>
    /// Gets the alert configuration for a given fuzzy system ID.
    /// </summary>
    /// <param name="fuzzySystemId">The fuzzy system ID to search for.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>The alert configuration if found, or null if not found.</returns>
    Task<WeatherAlertConfig?> GetByFuzzySystemIdAsync(string fuzzySystemId, CancellationToken ct = default);

    /// <summary>
    /// Gets all active alert configurations (e.g., those that are enabled and not expired).
    /// </summary>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>List of active alert configurations.</returns>
    Task<List<WeatherAlertConfig>> GetAllActiveAsync(CancellationToken ct = default);

    /// <summary>
    /// Creates a new alert configuration. The fuzzy system ID must be unique.
    /// </summary>
    /// <param name="config">The alert configuration to create.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>The created alert configuration with its assigned ID.</returns>
    Task<WeatherAlertConfig> CreateAsync(WeatherAlertConfig config, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing alert configuration identified by fuzzy system ID. If the config does not exist, returns null.
    /// </summary>
    /// <param name="fuzzySystemId">The fuzzy system ID of the config to update.</param>
    /// <param name="config">The updated alert configuration data.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>The updated alert configuration, or null if the config with the specified fuzzy system
    Task<WeatherAlertConfig?> UpdateAsync(string fuzzySystemId, WeatherAlertConfig config, CancellationToken ct = default);
  
    /// <summary>
    /// Deletes an alert configuration by fuzzy system ID. 
    /// Returns true if deletion was successful, false if not found.
    /// </summary>
    /// <param name="fuzzySystemId">The fuzzy system ID of the config to delete.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>True if the config was deleted, false if not found.</returns>
    Task<bool> DeleteAsync(string fuzzySystemId, CancellationToken ct = default);

    /// <summary> 
    /// Checks if an alert configuration exists for a given fuzzy system ID.
    /// </summary>
    /// <param name="fuzzySystemId">The fuzzy system ID to check for existence.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>True if a config exists for the given fuzzy system ID, otherwise false
    Task<bool> ExistsAsync(string fuzzySystemId, CancellationToken ct = default);
}
