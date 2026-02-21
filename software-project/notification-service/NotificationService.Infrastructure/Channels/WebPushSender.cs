using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Interfaces;
using NotificationService.Infrastructure.Options;
using WebPush;

namespace NotificationService.Infrastructure.Channels;

/// <summary>
/// Sends push notifications via Web Push Protocol (VAPID).
/// Uses the WebPush NuGet package. Requires VAPID keys configured.
/// </summary>
public class WebPushSender : INotificationChannel
{
    private readonly WebPushClient _webPushClient;
    private readonly VapidDetails? _vapidDetails;
    private readonly ILogger<WebPushSender> _logger;
    private readonly bool _isConfigured;

    public string ChannelType => NotificationChannels.WebPush;

    // Constructor reads VAPID keys from configuration and initializes WebPushClient
    public WebPushSender(IOptions<WebPushSettings> options, ILogger<WebPushSender> logger)
    {
        _logger = logger;
        _webPushClient = new WebPushClient();

        var settings = options.Value;
        if (!string.IsNullOrEmpty(settings.VapidPublicKey) &&
            !string.IsNullOrEmpty(settings.VapidPrivateKey) &&
            !string.IsNullOrEmpty(settings.VapidSubject))
        {
            _vapidDetails = new VapidDetails(
                settings.VapidSubject,
                settings.VapidPublicKey,
                settings.VapidPrivateKey);
            _isConfigured = true;
        }
        else
        {
            _logger.LogWarning("Web Push VAPID keys not configured. WebPushSender will be unavailable.");
            _isConfigured = false;
        }
    }

    // Web Push is available only if VAPID keys are configured
    public Task<bool> IsAvailableAsync(CancellationToken ct = default)
        => Task.FromResult(_isConfigured);

    /// <inheritdoc />
    public async Task<NotificationSendResult> SendAsync(NotificationMessage message, CancellationToken ct = default)
    {
        if (!_isConfigured)
        {
            return new NotificationSendResult(false, ChannelType, "vapid", null,
                "Web Push VAPID keys not configured");
        }

        if (string.IsNullOrEmpty(message.WebPushSubscription))
        {
            return new NotificationSendResult(false, ChannelType, "vapid", null,
                "No Web Push subscription provided");
        }

        try
        {
            // Parse the PushSubscription JSON stored in the token field
            var subJson = JsonSerializer.Deserialize<WebPushSubscriptionJson>(message.WebPushSubscription);
            if (subJson == null || string.IsNullOrEmpty(subJson.Endpoint))
            {
                return new NotificationSendResult(false, ChannelType, "vapid", null,
                    "Invalid Web Push subscription JSON");
            }

            var subscription = new WebPush.PushSubscription(
                subJson.Endpoint,
                subJson.Keys?.P256dh ?? "",
                subJson.Keys?.Auth ?? "");

            // Build the push payload
            var payload = JsonSerializer.Serialize(new
            {
                title = message.Title,
                body = message.Body,
                data = message.Data
            });

            await _webPushClient.SendNotificationAsync(subscription, payload, _vapidDetails);

            _logger.LogInformation("Web Push sent to endpoint {Endpoint}",
                subJson.Endpoint[..Math.Min(50, subJson.Endpoint.Length)]);
            return new NotificationSendResult(true, ChannelType, "vapid", null, null);
        }
        catch (WebPushException ex)
        {
            _logger.LogWarning(ex, "Web Push failed: {StatusCode} {Message}",
                ex.StatusCode, ex.Message);
            return new NotificationSendResult(false, ChannelType, "vapid", null,
                $"WebPush error {ex.StatusCode}: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception sending Web Push");
            return new NotificationSendResult(false, ChannelType, "vapid", null, ex.Message);
        }
    }

    // --- JSON deserialization for PushSubscription stored as token ---

    private class WebPushSubscriptionJson
    {
        public string? Endpoint { get; set; }
        public WebPushKeysJson? Keys { get; set; }
    }

    private class WebPushKeysJson
    {
        public string? P256dh { get; set; }
        public string? Auth { get; set; }
    }
}
