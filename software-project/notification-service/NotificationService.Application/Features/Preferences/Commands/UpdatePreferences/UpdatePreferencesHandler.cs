using MediatR;
using Microsoft.Extensions.Logging;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Interfaces;

namespace NotificationService.Application.Features.Preferences.Commands.UpdatePreferences;

public class UpdatePreferencesHandler : IRequestHandler<UpdatePreferencesCommand, NotificationPreference>
{
    private readonly INotificationPreferenceRepository _repository;
    private readonly IDailySummaryScheduler _scheduler;
    private readonly ILogger<UpdatePreferencesHandler> _logger;

    public UpdatePreferencesHandler(
        INotificationPreferenceRepository repository,
        IDailySummaryScheduler scheduler,
        ILogger<UpdatePreferencesHandler> logger)
    {
        _repository = repository;
        _scheduler = scheduler;
        _logger = logger;
    }

    public async Task<NotificationPreference> Handle(UpdatePreferencesCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Updating notification preferences for user {UserId}", request.UserId);

        // Get existing or create new
        var prefs = await _repository.GetByUserIdAsync(request.UserId);
        
        if (prefs == null)
        {
            _logger.LogDebug("No existing preferences found for user {UserId}, creating new document", request.UserId);
            prefs = new NotificationPreference { UserId = request.UserId };
        }

        // Map DTOs to Domain Entities
        prefs.Channels = request.Channels.Select(c => new ChannelPreference 
        { 
            Channel = c.Channel, 
            Enabled = c.Enabled,
            Target = c.Target
        }).ToList();

        prefs.DailySummary = new DailySummaryConfig
        {
            Enabled = request.DailySummary.Enabled,
            Hour = request.DailySummary.Hour,
            Minute = request.DailySummary.Minute,
            Channels = request.DailySummary.Channels,
            IncludeFuzzyRules = request.DailySummary.IncludeFuzzyRules,
            IncludeSensorAverages = request.DailySummary.IncludeSensorAverages,
            IncludeActuatorRuntime = request.DailySummary.IncludeActuatorRuntime,
            IncludeWeatherForecast = request.DailySummary.IncludeWeatherForecast
        };

        prefs.WeatherAlertsSubscription = new WeatherAlertSubscription
        {
            Enabled = request.WeatherAlertsSubscription.Enabled,
            FuzzySystemId = request.WeatherAlertsSubscription.FuzzySystemId,
            AlertTypes = request.WeatherAlertsSubscription.AlertTypes
        };

        if (request.QuietHours != null)
        {
            prefs.QuietHours = new QuietHoursConfig
            {
                Enabled = request.QuietHours.Enabled,
                StartHour = request.QuietHours.StartHour,
                EndHour = request.QuietHours.EndHour
            };
        }

        // Save
        if (string.IsNullOrEmpty(prefs.Id))
        {
            await _repository.CreateAsync(prefs, cancellationToken);
        }
        else
        {
            prefs.UpdateTimestamp();
            await _repository.UpdateAsync(prefs, cancellationToken);
        }

        // Synchronize Quartz schedule for Daily Summary
        try
        {
            if (prefs.DailySummary.Enabled)
            {
                await _scheduler.RescheduleAsync(
                    prefs.UserId, 
                    prefs.DailySummary.Hour, 
                    prefs.DailySummary.Minute, 
                    cancellationToken);
            }
            else
            {
                await _scheduler.RemoveAsync(prefs.UserId, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to synchronize Quartz schedule for user {UserId}", prefs.UserId);
        }

        return prefs;
    }
}

