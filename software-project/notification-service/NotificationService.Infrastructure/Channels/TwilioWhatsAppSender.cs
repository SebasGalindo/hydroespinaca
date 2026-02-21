using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Interfaces;
using NotificationService.Infrastructure.Options;

namespace NotificationService.Infrastructure.Channels;

/// <summary>
/// Sends WhatsApp messages via Twilio API.
/// Feature-flagged: if WhatsApp.Enabled=false, all sends return unavailable.
/// Uses Twilio REST API directly (no SDK dependency) for simplicity.
/// </summary>
public class TwilioWhatsAppSender : INotificationChannel
{
    private readonly HttpClient _httpClient;
    private readonly WhatsAppSettings _settings;
    private readonly ILogger<TwilioWhatsAppSender> _logger;
    private readonly bool _isEnabled;

    public string ChannelType => NotificationChannels.WhatsApp;

    // Constructor reads Twilio config from options and determines if channel is enabled
    public TwilioWhatsAppSender(
        IHttpClientFactory httpClientFactory,
        IOptions<WhatsAppSettings> options,
        ILogger<TwilioWhatsAppSender> logger)
    {
        _httpClient = httpClientFactory.CreateClient("TwilioWhatsApp");
        _settings = options.Value;
        _logger = logger;
        _isEnabled = _settings.Enabled &&
                     !string.IsNullOrEmpty(_settings.Twilio.AccountSid) &&
                     !string.IsNullOrEmpty(_settings.Twilio.AuthToken) &&
                     !string.IsNullOrEmpty(_settings.Twilio.FromNumber);

        if (!_isEnabled)
            _logger.LogInformation("WhatsApp channel is disabled or not configured.");
    }

    // WhatsApp is available only if enabled and Twilio config is present
    public Task<bool> IsAvailableAsync(CancellationToken ct = default)
        => Task.FromResult(_isEnabled);

    /// <inheritdoc />
    public async Task<NotificationSendResult> SendAsync(NotificationMessage message, CancellationToken ct = default)
    {
        if (!_isEnabled)
        {
            return new NotificationSendResult(false, ChannelType, "twilio", null,
                "WhatsApp channel is disabled");
        }

        if (string.IsNullOrEmpty(message.RecipientPhone))
        {
            return new NotificationSendResult(false, ChannelType, "twilio", null,
                "No recipient phone number provided");
        }

        try
        {
            var url = $"https://api.twilio.com/2010-04-01/Accounts/{_settings.Twilio.AccountSid}/Messages.json";

            // Format: "whatsapp:+573001234567"
            var fromNumber = _settings.Twilio.FromNumber.StartsWith("whatsapp:")
                ? _settings.Twilio.FromNumber
                : $"whatsapp:{_settings.Twilio.FromNumber}";
            var toNumber = message.RecipientPhone.StartsWith("whatsapp:")
                ? message.RecipientPhone
                : $"whatsapp:{message.RecipientPhone}";

            var formData = new FormUrlEncodedContent(
            [
                new KeyValuePair<string, string>("From", fromNumber),
                new KeyValuePair<string, string>("To", toNumber),
                new KeyValuePair<string, string>("Body", $"*{message.Title}*\n\n{message.Body}")
            ]);

            // Basic Auth: AccountSid:AuthToken
            var authBytes = Encoding.ASCII.GetBytes(
                $"{_settings.Twilio.AccountSid}:{_settings.Twilio.AuthToken}");
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Basic", Convert.ToBase64String(authBytes));

            var response = await _httpClient.PostAsync(url, formData, ct);
            var responseBody = await response.Content.ReadAsStringAsync(ct);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("WhatsApp sent to {Phone}", message.RecipientPhone);
                return new NotificationSendResult(true, ChannelType, "twilio", null, null);
            }

            _logger.LogWarning("Twilio WhatsApp failed: {StatusCode} {Body}",
                response.StatusCode, responseBody);
            return new NotificationSendResult(false, ChannelType, "twilio", null,
                $"HTTP {response.StatusCode}: {responseBody}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception sending WhatsApp to {Phone}", message.RecipientPhone);
            return new NotificationSendResult(false, ChannelType, "twilio", null, ex.Message);
        }
    }
}
