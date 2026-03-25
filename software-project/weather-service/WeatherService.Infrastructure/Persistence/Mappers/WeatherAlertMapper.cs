using HydroEspinaca.Shared.Mongo.Interfaces;
using WeatherService.Domain.Entities;
using WeatherService.Domain.Persistence.Documents;

namespace WeatherService.Infrastructure.Persistence.Mappers;

/// <summary>
/// Mapper for converting between WeatherAlert domain entity and WeatherAlertDocument used for MongoDB persistence. 
/// </summary>
public class WeatherAlertMapper : IEntityMapper<WeatherAlert, WeatherAlertDocument>
{
    /// <summary>
    /// Maps a WeatherAlertDocument from MongoDB to the WeatherAlert domain entity used in the application. 
    /// </summary>
    public WeatherAlert ToEntity(WeatherAlertDocument d) => new()
    {
        Id = d.Id,
        FuzzySystemId = d.FuzzySystemId,
        AlertType = d.AlertType,
        Severity = d.Severity,
        Title = d.Title,
        Message = d.Message,
        Recommendation = d.Recommendation,
        ForecastDatetime = d.ForecastDatetime,
        ForecastValue = d.ForecastValue,
        ForecastCondition = d.ForecastCondition,
        GovernmentAlert = d.GovernmentAlert,
        NotifiedUsers = d.NotifiedUsers.Select(n => new NotifiedUser
        {
            UserId = n.UserId,
            Channels = n.Channels,
            SentAt = n.SentAt,
            IsRead = n.IsRead
        }).ToList(),
        CreatedAt = d.CreatedAt
    };

    /// <summary>
    /// Maps a WeatherAlert domain entity to a WeatherAlertDocument for MongoDB persistence.
    /// </summary>
    public WeatherAlertDocument ToDocument(WeatherAlert e) => new()
    {
        Id = e.Id,
        FuzzySystemId = e.FuzzySystemId,
        AlertType = e.AlertType,
        Severity = e.Severity,
        Title = e.Title,
        Message = e.Message,
        Recommendation = e.Recommendation,
        ForecastDatetime = e.ForecastDatetime,
        ForecastValue = e.ForecastValue,
        ForecastCondition = e.ForecastCondition,
        GovernmentAlert = e.GovernmentAlert,
        NotifiedUsers = e.NotifiedUsers.Select(n => new NotifiedUserDocument
        {
            UserId = n.UserId,
            Channels = n.Channels,
            SentAt = n.SentAt,
            IsRead = n.IsRead
        }).ToList(),
        CreatedAt = e.CreatedAt
    };
}
