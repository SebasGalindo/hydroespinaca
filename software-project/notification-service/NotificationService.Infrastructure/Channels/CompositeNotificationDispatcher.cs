using Microsoft.Extensions.Logging;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Interfaces;

namespace NotificationService.Infrastructure.Channels;

/// <summary>
/// Orchestrates multi-channel notification delivery.
/// For each user, looks up their preferences, resolves push tokens if needed,
/// and dispatches to all enabled and available channels in parallel.
/// </summary>
public class CompositeNotificationDispatcher : INotificationDispatcher
{
    private readonly IEnumerable<INotificationChannel> _channels;
    private readonly INotificationPreferenceRepository _preferenceRepository;
    private readonly IPushSubscriptionRepository _pushSubscriptionRepository;
    private readonly INotificationLogRepository _logRepository;
    private readonly ILogger<CompositeNotificationDispatcher> _logger;

    // Constructor with dependency injection for channels, repositories, and logger    
    public CompositeNotificationDispatcher(
        IEnumerable<INotificationChannel> channels,
        INotificationPreferenceRepository preferenceRepository,
        IPushSubscriptionRepository pushSubscriptionRepository,
        INotificationLogRepository logRepository,
        ILogger<CompositeNotificationDispatcher> logger)
    {
        _channels = channels;
        _preferenceRepository = preferenceRepository;
        _pushSubscriptionRepository = pushSubscriptionRepository;
        _logRepository = logRepository;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<NotificationSendResult>> DispatchAsync(
        string userId,
        string templateKey,
        string title,
        string body,
        Dictionary<string, string>? data = null,
        List<string>? allowedChannels = null,
        CancellationToken ct = default)
    {
        var correlationId = Guid.NewGuid().ToString("N");
        var results = new List<NotificationSendResult>();

        // 1. Get user preferences (create default if none exist)
        var prefs = await _preferenceRepository.GetByUserIdAsync(userId, ct);
        if (prefs == null)
        {
            _logger.LogInformation("No preferences found for user {UserId}, using defaults (email only)", userId);
            prefs = CreateDefaultPreferences(userId);
            await _preferenceRepository.CreateAsync(prefs, ct);
        }

        // 2. Check quiet hours
        if (IsInQuietHours(prefs))
        {
            _logger.LogInformation("User {UserId} is in quiet hours, skipping push/whatsapp", userId);
        }

        // Log enabled channels for this user
        var enabledChannels = prefs.GetEnabledChannels();
        if (allowedChannels != null && allowedChannels.Count > 0)
        {
            enabledChannels = enabledChannels.Intersect(allowedChannels).ToList();
        }
        _logger.LogInformation("Dispatching notification to user {UserId} via channels: {Channels}",
            userId, string.Join(", ", enabledChannels));

        // 3. Send to each enabled channel
        foreach (var channelName in enabledChannels)
        {
            // Skip push/whatsapp during quiet hours (email goes through)
            if (IsInQuietHours(prefs) && channelName != NotificationChannels.Email)
                continue;

            // Find the channel implementation
            var channel = _channels.FirstOrDefault(c => c.ChannelType == channelName);
            if (channel == null)
            {
                _logger.LogWarning("Channel {Channel} not registered, skipping", channelName);
                continue;
            }

            if (!await channel.IsAvailableAsync(ct))
            {
                _logger.LogWarning("Channel {Channel} is not available, skipping", channelName);
                continue;
            }

            // Resolve channel-specific targets and send
            var channelResults = await SendToChannelAsync(
                channel, userId, correlationId, templateKey, title, body, data, prefs, ct);
            results.AddRange(channelResults);
        }

        return results;
    }

    /// <inheritdoc />
    private async Task<List<NotificationSendResult>> SendToChannelAsync(
        INotificationChannel channel,
        string userId,
        string correlationId,
        string templateKey,
        string title,
        string body,
        Dictionary<string, string>? data,
        NotificationPreference prefs,
        CancellationToken ct)
    {
        var results = new List<NotificationSendResult>();

        // For each channel, we need to build a NotificationMessage with the appropriate target (email, push token, phone number, etc.)
        switch (channel.ChannelType)
        {
            // For email we look up the target in user preferences.
            case NotificationChannels.Email:
            {
                var emailTarget = prefs.Channels
                    .FirstOrDefault(c => c.Channel == NotificationChannels.Email)?.Target;
                if (string.IsNullOrEmpty(emailTarget))
                {
                    _logger.LogWarning("No email target for user {UserId}", userId);
                    break;
                }

                var msg = BuildMessage(correlationId, userId, channel.ChannelType, templateKey, title, body, data);
                msg.RecipientEmail = emailTarget;
                var result = await SendAndLogAsync(channel, msg, ct);
                results.Add(result);
                break;
            }

            case NotificationChannels.Push:
            {
                // Send to ALL active Expo push tokens for this user
                var expoSubs = await _pushSubscriptionRepository
                    .GetActiveByUserIdAsync(userId, "expo", ct);
                foreach (var sub in expoSubs)
                {
                    var msg = BuildMessage(correlationId, userId, channel.ChannelType, templateKey, title, body, data);
                    msg.ExpoPushToken = sub.Token;
                    var result = await SendAndLogAsync(channel, msg, ct);
                    results.Add(result);

                    // Update subscription status
                    if (result.Success) sub.RecordSuccess();
                    else sub.RecordFailure();
                    await _pushSubscriptionRepository.UpdateAsync(sub, ct);
                }
                break;
            }

            case NotificationChannels.WebPush:
            {
                var webPushBody = body;
                if (templateKey == "weather_alert")
                {
                    var countStr = data != null && data.TryGetValue("alertCount", out var c) ? c : "1";
                    var isPlural = countStr != "1";
                    webPushBody = $"Tienes {countStr} nueva{(isPlural ? "s" : "")} alerta{(isPlural ? "s" : "")} meteorológica{(isPlural ? "s" : "")}, entra a la web y toma las medidas necesarias para mantener el invernadero adecuadamente.";
                }

                _logger.LogInformation("[CompositeDispatcher] Getting active Web Push subscriptions for user {UserId}", userId);
                // Send to ALL active Web Push subscriptions for this user
                var webSubs = await _pushSubscriptionRepository
                    .GetActiveByUserIdAsync(userId, "web_push", ct);
                
                _logger.LogInformation("[CompositeDispatcher] Found {Count} active Web Push subscriptions for user {UserId}", webSubs.Count(), userId);

                foreach (var sub in webSubs)
                {
                    _logger.LogInformation("[CompositeDispatcher] Processing Web Push sub: {SubId}", sub.Id);
                    var msg = BuildMessage(correlationId, userId, channel.ChannelType, templateKey, title, webPushBody, data);
                    msg.WebPushSubscription = sub.Token;
                    var result = await SendAndLogAsync(channel, msg, ct);
                    results.Add(result);

                    if (result.Success) 
                    {
                        _logger.LogInformation("[CompositeDispatcher] Web Push to {SubId} succeeded", sub.Id);
                        sub.RecordSuccess();
                    }
                    else 
                    {
                        _logger.LogWarning("[CompositeDispatcher] Web Push to {SubId} failed: {Error}", sub.Id, result.Error);
                        sub.RecordFailure();
                    }
                    await _pushSubscriptionRepository.UpdateAsync(sub, ct);
                }
                break;
            }

            case NotificationChannels.WhatsApp:
            {
                var phoneTarget = prefs.Channels
                    .FirstOrDefault(c => c.Channel == NotificationChannels.WhatsApp)?.Target;
                if (string.IsNullOrEmpty(phoneTarget))
                {
                    _logger.LogWarning("No WhatsApp phone for user {UserId}", userId);
                    break;
                }

                var msg = BuildMessage(correlationId, userId, channel.ChannelType, templateKey, title, body, data);
                msg.RecipientPhone = phoneTarget;
                var result = await SendAndLogAsync(channel, msg, ct);
                results.Add(result);
                break;
            }
        }

        return results;
    }

    /// <inheritdoc />
    private async Task<NotificationSendResult> SendAndLogAsync(
        INotificationChannel channel, NotificationMessage message, CancellationToken ct)
    {
        // Create log entry before send
        var log = new NotificationLog
        {
            CorrelationId = message.CorrelationId,
            UserId = message.UserId,
            Channel = channel.ChannelType,
            TemplateKey = message.TemplateKey,
            Title = message.Title,
            Status = "queued"
        };
        await _logRepository.CreateAsync(log, ct);

        // Send
        var result = await channel.SendAsync(message, ct);

        // Update log
        if (result.Success)
            log.MarkSent(result.Provider ?? channel.ChannelType, result.ProviderMessageId);
        else
            log.MarkFailed(result.Provider, result.Error ?? "Unknown error");
        await _logRepository.UpdateAsync(log, ct);

        return result;
    }

    /// <inheritdoc />
    private static NotificationMessage BuildMessage(
        string correlationId, string userId, string channel,
        string templateKey, string title, string body,
        Dictionary<string, string>? data) => new()
    {
        CorrelationId = correlationId,
        UserId = userId,
        Channel = channel,
        TemplateKey = templateKey,
        Title = title,
        Body = body,
        Data = data ?? new()
    };

    /// <inheritdoc />
    private static NotificationPreference CreateDefaultPreferences(string userId) => new()
    {
        UserId = userId,
        Channels =
        [
            new ChannelPreference { Channel = NotificationChannels.Email, Enabled = true },
            new ChannelPreference { Channel = NotificationChannels.Push, Enabled = false },
            new ChannelPreference { Channel = NotificationChannels.WebPush, Enabled = false },
            new ChannelPreference { Channel = NotificationChannels.WhatsApp, Enabled = false }
        ],
        DailySummary = new DailySummaryConfig { Enabled = false },
        WeatherAlertsSubscription = new WeatherAlertSubscription { Enabled = true }
    };

    /// <inheritdoc />
    private static bool IsInQuietHours(NotificationPreference prefs)
    {
        if (prefs.QuietHours == null || !prefs.QuietHours.Enabled)
            return false;

        // Colombian time: UTC-5
        var nowLocal = DateTime.UtcNow.AddHours(-5);
        var hour = nowLocal.Hour;

        var start = prefs.QuietHours.StartHour;
        var end = prefs.QuietHours.EndHour;

        // Handle overnight quiet hours (e.g., 22:00 - 07:00)
        if (start > end)
            return hour >= start || hour < end;
        return hour >= start && hour < end;
    }
}
