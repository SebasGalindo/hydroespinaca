using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Interfaces;
using NotificationService.Infrastructure.Options;

namespace NotificationService.Infrastructure.Channels;

/// <summary>
/// Sends WhatsApp messages via Meta Cloud API (WhatsApp Business Platform).
/// Uses pre-approved message templates with positional variables.
/// Feature-flagged: requires WhatsApp.Enabled=true and Provider=MetaCloud.
/// </summary>
public class MetaCloudWhatsAppSender : INotificationChannel
{
    private readonly HttpClient _httpClient;
    private readonly MetaCloudSettings _metaSettings;
    private readonly ILogger<MetaCloudWhatsAppSender> _logger;
    private readonly bool _isEnabled;
    private readonly JsonSerializerOptions _jsonOptions;

    public string ChannelType => NotificationChannels.WhatsApp;

    public MetaCloudWhatsAppSender(
        IHttpClientFactory httpClientFactory,
        IOptions<WhatsAppSettings> whatsAppOptions,
        IOptions<MetaCloudSettings> metaOptions,
        ILogger<MetaCloudWhatsAppSender> logger)
    {
        _httpClient = httpClientFactory.CreateClient("MetaCloudWhatsApp");
        _metaSettings = metaOptions.Value;
        _logger = logger;

        var wa = whatsAppOptions.Value;
        _isEnabled = wa.Enabled &&
                     wa.Provider == WhatsAppProvider.MetaCloud &&
                     !string.IsNullOrEmpty(_metaSettings.PhoneNumberId) &&
                     !string.IsNullOrEmpty(_metaSettings.AccessToken);

        if (!_isEnabled)
            _logger.LogInformation("MetaCloud WhatsApp channel is disabled or not configured.");

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
    }

    public Task<bool> IsAvailableAsync(CancellationToken ct = default)
        => Task.FromResult(_isEnabled);

    /// <inheritdoc />
    public async Task<NotificationSendResult> SendAsync(NotificationMessage message, CancellationToken ct = default)
    {
        if (!_isEnabled)
        {
            return new NotificationSendResult(false, ChannelType, "meta_cloud", null,
                "MetaCloud WhatsApp channel is disabled");
        }

        if (string.IsNullOrEmpty(message.RecipientPhone))
        {
            return new NotificationSendResult(false, ChannelType, "meta_cloud", null,
                "No recipient phone number provided");
        }

        var templateName = MetaWhatsAppTemplateMapper.GetTemplateName(message.TemplateKey);
        if (templateName == null)
        {
            _logger.LogWarning("No Meta template mapped for templateKey={TemplateKey}, falling back to plain text body",
                message.TemplateKey);
            // For unmapped templates, we can't send via Meta (templates are required)
            return new NotificationSendResult(false, ChannelType, "meta_cloud", null,
                $"No Meta WhatsApp template mapped for templateKey '{message.TemplateKey}'");
        }

        try
        {
            // Build API URL
            var url = $"https://graph.facebook.com/{_metaSettings.ApiVersion}/{_metaSettings.PhoneNumberId}/messages";

            // Clean phone number: remove "whatsapp:" prefix if present, keep only digits and +
            var phone = message.RecipientPhone
                .Replace("whatsapp:", "", StringComparison.OrdinalIgnoreCase)
                .Trim();

            // Build template components
            var headerParams = MetaWhatsAppTemplateMapper.BuildHeaderParameters(message.TemplateKey, message);
            var bodyParams = MetaWhatsAppTemplateMapper.BuildBodyParameters(message.TemplateKey, message);

            var components = new List<object>();

            if (headerParams.Count > 0)
            {
                components.Add(new
                {
                    type = "header",
                    parameters = headerParams.Select(p => new { type = "text", text = p.Text }).ToArray()
                });
            }

            if (bodyParams.Count > 0)
            {
                components.Add(new
                {
                    type = "body",
                    parameters = bodyParams.Select(p => new { type = "text", text = p.Text }).ToArray()
                });
            }

            var payload = new
            {
                messaging_product = "whatsapp",
                to = phone,
                type = "template",
                template = new
                {
                    name = templateName,
                    language = new { code = MetaWhatsAppTemplateMapper.GetLanguageCode(message.TemplateKey) },
                    components
                }
            };

            var json = JsonSerializer.Serialize(payload, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _metaSettings.AccessToken);

            var response = await _httpClient.PostAsync(url, content, ct);
            var responseBody = await response.Content.ReadAsStringAsync(ct);

            if (response.IsSuccessStatusCode)
            {
                // Extract message ID from response
                var messageId = ExtractMessageId(responseBody);
                _logger.LogInformation(
                    "MetaCloud WhatsApp sent to {Phone} using template {Template} (messageId={MessageId})",
                    phone, templateName, messageId);
                return new NotificationSendResult(true, ChannelType, "meta_cloud", messageId, null);
            }

            _logger.LogWarning(
                "MetaCloud WhatsApp failed: {StatusCode} {Body}",
                response.StatusCode, responseBody);
            return new NotificationSendResult(false, ChannelType, "meta_cloud", null,
                $"HTTP {response.StatusCode}: {responseBody}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception sending MetaCloud WhatsApp to {Phone}", message.RecipientPhone);
            return new NotificationSendResult(false, ChannelType, "meta_cloud", null, ex.Message);
        }
    }

    /// <summary>
    /// Extracts the message ID from the Meta Cloud API response.
    /// Response format: { "messaging_product": "whatsapp", "contacts": [...], "messages": [{ "id": "wamid.xxx" }] }
    /// </summary>
    private static string? ExtractMessageId(string responseBody)
    {
        try
        {
            using var doc = JsonDocument.Parse(responseBody);
            if (doc.RootElement.TryGetProperty("messages", out var messages) &&
                messages.GetArrayLength() > 0)
            {
                return messages[0].GetProperty("id").GetString();
            }
        }
        catch
        {
            // Non-critical — we just won't have a message ID in the log
        }
        return null;
    }
}
