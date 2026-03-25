using HydroEspinaca.Shared.Abstractions;
using HydroEspinaca.Shared.Mongo.Interfaces;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Persistence.Documents;

namespace NotificationService.Infrastructure.Persistence.Mappers;

/// <summary>
/// Mapper for converting between NotificationPreference domain entities and NotificationPreferenceDocument MongoDB documents.
/// </summary>
public class NotificationPreferenceMapper : IEntityMapper<NotificationPreference, NotificationPreferenceDocument>
{
    /// <summary>
    /// Converts a NotificationPreferenceDocument from MongoDB into a NotificationPreference domain entity, which is used in the application logic.
    /// </summary>
    public NotificationPreference ToEntity(NotificationPreferenceDocument document) => new()
    {
        Id = document.Id,
        UserId = document.UserId,
        Channels = document.Channels.Select(c => new ChannelPreference
        {
            Channel = c.Channel,
            Enabled = c.Enabled,
            Target = c.Target
        }).ToList(),
        DailySummary = new DailySummaryConfig
        {
            Enabled = document.DailySummary.Enabled,
            Hour = document.DailySummary.Hour,
            Minute = document.DailySummary.Minute,
            Channels = document.DailySummary.Channels,
            IncludeFuzzyRules = document.DailySummary.IncludeFuzzyRules,
            IncludeSensorAverages = document.DailySummary.IncludeSensorAverages,
            IncludeActuatorRuntime = document.DailySummary.IncludeActuatorRuntime,
            IncludeWeatherForecast = document.DailySummary.IncludeWeatherForecast
        },
        WeatherAlertsSubscription = new WeatherAlertSubscription
        {
            Enabled = document.WeatherAlertsSubscription.Enabled,
            FuzzySystemId = document.WeatherAlertsSubscription.FuzzySystemId,
            AlertTypes = document.WeatherAlertsSubscription.AlertTypes
        },
        QuietHours = document.QuietHours != null ? new QuietHoursConfig
        {
            Enabled = document.QuietHours.Enabled,
            StartHour = document.QuietHours.StartHour,
            EndHour = document.QuietHours.EndHour
        } : null,
        CreatedAt = document.CreatedAt,
        UpdatedAt = document.UpdatedAt
    };

    /// <summary>
    /// Converts a NotificationPreference domain entity into a NotificationPreferenceDocument for storage in MongoDB. This is used when saving or updating notification preferences in the database.
    /// </summary>
    public NotificationPreferenceDocument ToDocument(NotificationPreference entity) => new()
    {
        Id = entity.Id,
        UserId = entity.UserId,
        Channels = entity.Channels.Select(c => new ChannelPreferenceDocument
        {
            Channel = c.Channel,
            Enabled = c.Enabled,
            Target = c.Target
        }).ToList(),
        DailySummary = new DailySummaryConfigDocument
        {
            Enabled = entity.DailySummary.Enabled,
            Hour = entity.DailySummary.Hour,
            Minute = entity.DailySummary.Minute,
            Channels = entity.DailySummary.Channels,
            IncludeFuzzyRules = entity.DailySummary.IncludeFuzzyRules,
            IncludeSensorAverages = entity.DailySummary.IncludeSensorAverages,
            IncludeActuatorRuntime = entity.DailySummary.IncludeActuatorRuntime,
            IncludeWeatherForecast = entity.DailySummary.IncludeWeatherForecast
        },
        WeatherAlertsSubscription = new WeatherAlertSubscriptionDocument
        {
            Enabled = entity.WeatherAlertsSubscription.Enabled,
            FuzzySystemId = entity.WeatherAlertsSubscription.FuzzySystemId,
            AlertTypes = entity.WeatherAlertsSubscription.AlertTypes
        },
        QuietHours = entity.QuietHours != null ? new QuietHoursConfigDocument
        {
            Enabled = entity.QuietHours.Enabled,
            StartHour = entity.QuietHours.StartHour,
            EndHour = entity.QuietHours.EndHour
        } : null,
        CreatedAt = entity.CreatedAt,
        UpdatedAt = entity.UpdatedAt
    };
}
