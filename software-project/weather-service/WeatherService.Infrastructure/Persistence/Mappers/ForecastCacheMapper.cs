using HydroEspinaca.Shared.Mongo.Interfaces;
using WeatherService.Domain.Entities;
using WeatherService.Domain.Persistence.Documents;

namespace WeatherService.Infrastructure.Persistence.Mappers;

/// <summary>
/// Mapper for converting between ForecastCache domain entity and ForecastCacheDocument used for MongoDB persistence.
/// </summary>
public class ForecastCacheMapper : IEntityMapper<ForecastCache, ForecastCacheDocument>
{
    /// <summary>
    /// Maps a ForecastCacheDocument from MongoDB to the ForecastCache domain entity used in the application.
    /// </summary>
    public ForecastCache ToEntity(ForecastCacheDocument d) => new()
    {
        Id = d.Id,
        FetchedAt = d.FetchedAt,
        ExpiresAt = d.ExpiresAt,
        Current = d.Current,
        Hourly = d.Hourly,
        Daily = d.Daily,
        GovernmentAlerts = d.GovernmentAlerts
    };

    /// <summary>
    /// Maps a ForecastCache domain entity to a ForecastCacheDocument for MongoDB persistence. This involves copying all relevant properties from the entity to the document, ensuring that the data is correctly structured for storage in MongoDB. The Id property is preserved to allow for updates to existing documents, and all forecast data (current, hourly, daily, government alerts) is included in the document representation.
    /// </summary>
    public ForecastCacheDocument ToDocument(ForecastCache e) => new()
    {
        Id = e.Id,
        FetchedAt = e.FetchedAt,
        ExpiresAt = e.ExpiresAt,
        Current = e.Current,
        Hourly = e.Hourly,
        Daily = e.Daily,
        GovernmentAlerts = e.GovernmentAlerts
    };
}
