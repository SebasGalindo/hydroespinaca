using HydroEspinaca.Shared.Mongo.Interfaces;
using WeatherService.Domain.Entities;
using WeatherService.Domain.Persistence.Documents;

namespace WeatherService.Infrastructure.Persistence.Mappers;

/// <summary>
/// Mapper for converting between WeatherAlertConfig domain entity and WeatherAlertConfigDocument used for MongoDB persistence.
/// </summary>
public class WeatherAlertConfigMapper : IEntityMapper<WeatherAlertConfig, WeatherAlertConfigDocument>
{
    /// <summary>
    /// Maps a WeatherAlertConfigDocument from MongoDB to the WeatherAlertConfig domain entity used in the application. This involves copying all relevant properties from the document to the entity, including the list of alert thresholds. The mapping ensures that the entity is fully populated with data from the document, allowing it to be used seamlessly within the application's business logic and services.
    /// </summary>
    public WeatherAlertConfig ToEntity(WeatherAlertConfigDocument d) => new()
    {
        Id = d.Id,
        FuzzySystemId = d.FuzzySystemId,
        FuzzySystemName = d.FuzzySystemName,
        Alerts = d.Alerts.Select(a => new AlertThreshold
        {
            Type = a.Type,
            Enabled = a.Enabled,
            ThresholdValue = a.ThresholdValue,
            Comparison = a.Comparison,
            Recommendation = a.Recommendation
        }).ToList(),
        IsActive = d.IsActive,
        MaxForecastDays = d.MaxForecastDays,
        AllowDuplicateAlerts = d.AllowDuplicateAlerts,
        CreatedBy = d.CreatedBy,
        UpdatedBy = d.UpdatedBy,
        CreatedAt = d.CreatedAt,
        UpdatedAt = d.UpdatedAt
    };

    /// <summary>
    /// Maps a WeatherAlertConfig domain entity to a WeatherAlertConfigDocument for MongoDB persistence. This involves copying all relevant properties from the entity to the document, ensuring that the data is correctly structured for storage in MongoDB. The Id property is preserved to allow for updates to existing documents, and all alert configuration data (fuzzy system ID, name, alert thresholds, active status, audit fields) is included in the document representation.
    /// </summary>
    public WeatherAlertConfigDocument ToDocument(WeatherAlertConfig e) => new()
    {
        Id = e.Id,
        FuzzySystemId = e.FuzzySystemId,
        FuzzySystemName = e.FuzzySystemName,
        Alerts = e.Alerts.Select(a => new AlertThresholdDocument
        {
            Type = a.Type,
            Enabled = a.Enabled,
            ThresholdValue = a.ThresholdValue,
            Comparison = a.Comparison,
            Recommendation = a.Recommendation
        }).ToList(),
        IsActive = e.IsActive,
        MaxForecastDays = e.MaxForecastDays,
        AllowDuplicateAlerts = e.AllowDuplicateAlerts,
        CreatedBy = e.CreatedBy,
        UpdatedBy = e.UpdatedBy,
        CreatedAt = e.CreatedAt,
        UpdatedAt = e.UpdatedAt
    };
}
