using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Interfaces;

namespace NotificationService.Infrastructure.Channels;

/// <summary>
/// Sends push notifications via Expo Push API (for mobile apps using Expo SDK).
/// POST https://exp.host/--/api/v2/push/send
/// Batch: up to 100 tokens per request.
/// </summary>
public class ExpoPushSender : INotificationChannel
{
    private const string ExpoApiUrl = "https://exp.host/--/api/v2/push/send";
    private readonly HttpClient _httpClient;
    private readonly ILogger<ExpoPushSender> _logger;

    public string ChannelType => NotificationChannels.Push;

    public ExpoPushSender(IHttpClientFactory httpClientFactory, ILogger<ExpoPushSender> logger)
    {
        _httpClient = httpClientFactory.CreateClient("ExpoPush");
        _logger = logger;
    }

    // Expo Push API is always available (no config needed)
    public Task<bool> IsAvailableAsync(CancellationToken ct = default)
        => Task.FromResult(true); // Expo Push API is always available (no config needed)

    /// <inheritdoc />
    public async Task<NotificationSendResult> SendAsync(NotificationMessage message, CancellationToken ct = default)
    {
        // Validate required fields
        if (string.IsNullOrEmpty(message.ExpoPushToken))
        {
            return new NotificationSendResult(false, ChannelType, "expo", null,
                "No Expo push token provided");
        }

        try
        {
            // Build the payload according to Expo Push API spec
            var payload = new ExpoPushPayload
            {
                To = message.ExpoPushToken,
                Title = message.Title,
                Body = message.Body,
                Sound = "default",
                Data = message.Data
            };
            
            // Send the POST request to Expo API
            var response = await _httpClient.PostAsJsonAsync(ExpoApiUrl, payload, ct);
            var responseBody = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Expo Push API returned {StatusCode}: {Body}",
                    response.StatusCode, responseBody);
                return new NotificationSendResult(false, ChannelType, "expo", null,
                    $"HTTP {response.StatusCode}: {responseBody}");
            }

            var result = JsonSerializer.Deserialize<ExpoPushResponse>(responseBody);
            var ticket = result?.Data?.FirstOrDefault();

            if (ticket?.Status == "ok")
            {
                _logger.LogInformation("Expo push sent to {Token}, ticket: {TicketId}",
                    message.ExpoPushToken[..Math.Min(30, message.ExpoPushToken.Length)],
                    ticket.Id);
                return new NotificationSendResult(true, ChannelType, "expo", ticket.Id, null);
            }

            var errorMsg = ticket?.Message ?? "Unknown error";
            _logger.LogWarning("Expo push failed for {Token}: {Error}",
                message.ExpoPushToken[..Math.Min(30, message.ExpoPushToken.Length)], errorMsg);
            return new NotificationSendResult(false, ChannelType, "expo", null, errorMsg);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception sending Expo push to {Token}",
                message.ExpoPushToken?[..Math.Min(30, message.ExpoPushToken?.Length ?? 0)]);
            return new NotificationSendResult(false, ChannelType, "expo", null, ex.Message);
        }
    }

    // --- Expo API DTOs ---

    private class ExpoPushPayload
    {
        [JsonPropertyName("to")] public string To { get; set; } = string.Empty;
        [JsonPropertyName("title")] public string Title { get; set; } = string.Empty;
        [JsonPropertyName("body")] public string Body { get; set; } = string.Empty;
        [JsonPropertyName("sound")] public string Sound { get; set; } = "default";
        [JsonPropertyName("data")] public Dictionary<string, string>? Data { get; set; }
    }

    private class ExpoPushResponse
    {
        [JsonPropertyName("data")] public List<ExpoPushTicket>? Data { get; set; }
    }

    private class ExpoPushTicket
    {
        [JsonPropertyName("id")] public string? Id { get; set; }
        [JsonPropertyName("status")] public string? Status { get; set; }
        [JsonPropertyName("message")] public string? Message { get; set; }
    }
}
