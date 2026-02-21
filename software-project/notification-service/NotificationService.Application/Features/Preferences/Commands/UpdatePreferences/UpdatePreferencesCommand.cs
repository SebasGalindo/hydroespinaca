using MediatR;
using NotificationService.Application.DTOs;
using NotificationService.Domain.Entities;

namespace NotificationService.Application.Features.Preferences.Commands.UpdatePreferences;

public record UpdatePreferencesCommand(
    string UserId,
    List<ChannelPreferenceDto> Channels,
    DailySummaryConfigDto DailySummary,
    WeatherAlertSubscriptionDto WeatherAlertsSubscription,
    QuietHoursConfigDto? QuietHours
) : IRequest<NotificationPreference>;
