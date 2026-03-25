using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using BffService.Application.Interfaces;
using BffService.Domain.DTOs;
using BffService.Domain.Interfaces;
using BffService.Domain.Exceptions;

namespace BffService.Api.Controllers;

/// <summary>
/// Proxies notification-service endpoints for preferences, push subscriptions and history.
/// </summary>
[ApiController]
[Route("notifications")]
[AllowAnonymous] // We'll validate session manually via BaseAuthenticatedController
public class NotificationProxyController : BaseAuthenticatedController
{
    private readonly INotificationServiceClient _client;

    public NotificationProxyController(
        INotificationServiceClient client,
        ISessionTokenService sessionTokenService,
        IConfiguration configuration,
        ILogger<NotificationProxyController> logger)
        : base(sessionTokenService, configuration, logger)
    {
        _client = client;
    }

    // ──────────────── Preferences ────────────────

    /// <summary>
    /// Gets notification preferences for a user.
    /// Returns default preferences if none are found.
    /// </summary>
    [HttpGet("preferences/{userId}")]
    public async Task<IActionResult> GetPreferences(string userId, CancellationToken ct)
    {
        try
        {
            var session = await ValidateSessionAsync(ct);
            var result = await _client.GetPreferencesAsync(userId, session.AccessToken, ct);
            
            if (result == null)
            {
                Logger.LogInformation("No notification preferences found for user {UserId}, returning defaults", userId);

                // Check if user has push subscriptions to set sensible defaults
                var pushSubs = await _client.GetPushSubscriptionsAsync(userId, session.AccessToken, null, ct);
                var hasWebPush = pushSubs != null && pushSubs.Any(p => p.Platform == "web_push" && p.IsActive);
                var hasMobilePush = pushSubs != null && pushSubs.Any(p => p.Platform == "expo" && p.IsActive);

                return Ok(new NotificationPreferenceDto
                {
                    UserId = userId,
                    Channels = new List<ChannelPreferenceDto>
                    {
                        new() { Channel = "email", Enabled = true, Target = session.Email },
                        new() { Channel = "push", Enabled = hasMobilePush },
                        new() { Channel = "web_push", Enabled = hasWebPush },
                        new() { Channel = "whatsapp", Enabled = false }
                    },
                    DailySummary = new DailySummaryConfigDto
                    {
                        Enabled = false,
                        Hour = 7,
                        Minute = 0,
                        Channels = new List<string> { "email" }
                    },
                    WeatherAlertsSubscription = new WeatherAlertSubscriptionDto
                    {
                        Enabled = true,
                        AlertTypes = new List<string>()
                    },
                    QuietHours = new QuietHoursConfigDto
                    {
                        Enabled = false,
                        StartHour = 22,
                        EndHour = 7
                    }
                });
            }
            
            // If preferences exist but email target is missing, patch it from session
            var emailChannel = result.Channels?.FirstOrDefault(c => c.Channel == "email");
            if (emailChannel != null && string.IsNullOrEmpty(emailChannel.Target))
            {
                emailChannel.Target = session.Email;
            }
            
            return Ok(result);
        }
        catch (SessionNotFoundException) { return Unauthorized(new { message = "Sesión no encontrada" }); }
        catch (SessionExpiredException) { return Unauthorized(new { message = "Sesión expirada" }); }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error getting notification preferences for {UserId}", userId);
            return StatusCode(500, new { message = "Error al obtener preferencias de notificación" });
        }
    }

    /// <summary>
    /// Updates (or creates) notification preferences for a user.
    /// </summary>
    [HttpPut("preferences/{userId}")]
    public async Task<IActionResult> UpdatePreferences(
        string userId, [FromBody] UpdatePreferencesRequestDto request, CancellationToken ct)
    {
        try
        {
            var session = await ValidateSessionAsync(ct);
            
            // Ensure email target is populated from session if missing in the request
            var emailChannel = request.Channels?.FirstOrDefault(c => c.Channel == "email");
            if (emailChannel != null && string.IsNullOrEmpty(emailChannel.Target))
            {
                emailChannel.Target = session.Email;
            }
            
            var result = await _client.UpdatePreferencesAsync(userId, request, session.AccessToken, ct);
            return Ok(result);
        }
        catch (SessionNotFoundException) { return Unauthorized(new { message = "Sesión no encontrada" }); }
        catch (SessionExpiredException) { return Unauthorized(new { message = "Sesión expirada" }); }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error updating notification preferences for {UserId}", userId);
            return StatusCode(500, new { message = "Error al actualizar preferencias de notificación" });
        }
    }

    // ──────────────── Push Subscriptions ────────────────

    /// <summary>
    /// Registers a push subscription for a user.
    /// </summary>
    [HttpPost("push/register")]
    public async Task<IActionResult> RegisterPush(
        [FromBody] RegisterPushRequestDto request, CancellationToken ct)
    {
        try
        {
            var session = await ValidateSessionAsync(ct);
            var result = await _client.RegisterPushAsync(request, session.AccessToken, ct);
            return Created($"notifications/push/subscriptions/{request.UserId}", result);
        }
        catch (SessionNotFoundException) { return Unauthorized(new { message = "Sesión no encontrada" }); }
        catch (SessionExpiredException) { return Unauthorized(new { message = "Sesión expirada" }); }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error registering push subscription");
            return StatusCode(500, new { message = "Error al registrar suscripción push" });
        }
    }

    /// <summary>
    /// Unregisters a push subscription by its ID.
    /// </summary>
    [HttpDelete("push/register/{subscriptionId}")]
    public async Task<IActionResult> UnregisterPush(string subscriptionId, CancellationToken ct)
    {
        try
        {
            var session = await ValidateSessionAsync(ct);
            await _client.UnregisterPushAsync(subscriptionId, session.AccessToken, ct);
            return NoContent();
        }
        catch (SessionNotFoundException) { return Unauthorized(new { message = "Sesión no encontrada" }); }
        catch (SessionExpiredException) { return Unauthorized(new { message = "Sesión expirada" }); }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error unregistering push subscription {SubscriptionId}", subscriptionId);
            return StatusCode(500, new { message = "Error al eliminar suscripción push" });
        }
    }

    /// <summary>
    /// Gets active push subscriptions for a user.
    /// </summary>
    [HttpGet("push/subscriptions/{userId}")]
    public async Task<IActionResult> GetPushSubscriptions(
        string userId, [FromQuery] string? platform, CancellationToken ct)
    {
        try
        {
            var session = await ValidateSessionAsync(ct);
            var result = await _client.GetPushSubscriptionsAsync(userId, session.AccessToken, platform, ct);
            return Ok(result);
        }
        catch (SessionNotFoundException) { return Unauthorized(new { message = "Sesión no encontrada" }); }
        catch (SessionExpiredException) { return Unauthorized(new { message = "Sesión expirada" }); }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error getting push subscriptions for {UserId}", userId);
            return StatusCode(500, new { message = "Error al obtener suscripciones push" });
        }
    }

    // ──────────────── Notification History ────────────────

    /// <summary>
    /// Gets notification history (log) for a user with optional filters.
    /// </summary>
    [HttpGet("log/{userId}")]
    public async Task<IActionResult> GetNotificationHistory(
        string userId,
        [FromQuery] string? channel,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int limit = 50,
        CancellationToken ct = default)
    {
        try
        {
            var session = await ValidateSessionAsync(ct);
            var result = await _client.GetNotificationHistoryAsync(userId, session.AccessToken, channel, from, to, limit, ct);
            return Ok(result);
        }
        catch (SessionNotFoundException) { return Unauthorized(new { message = "Sesión no encontrada" }); }
        catch (SessionExpiredException) { return Unauthorized(new { message = "Sesión expirada" }); }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error getting notification history for {UserId}", userId);
            return StatusCode(500, new { message = "Error al obtener historial de notificaciones" });
        }
    }

    // ──────────────── Multi-Channel Send ────────────────

    /// <summary>
    /// Sends a multi-channel notification (triggers all enabled channels for the user).
    /// </summary>
    [HttpPost("send")]
    public async Task<IActionResult> SendMultiChannel(
        [FromBody] SendMultiChannelRequestDto request, CancellationToken ct)
    {
        try
        {
            var session = await ValidateSessionAsync(ct);
            var result = await _client.SendMultiChannelAsync(request, session.AccessToken, ct);
            return Ok(result);
        }
        catch (SessionNotFoundException) { return Unauthorized(new { message = "Sesión no encontrada" }); }
        catch (SessionExpiredException) { return Unauthorized(new { message = "Sesión expirada" }); }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error sending multi-channel notification");
            return StatusCode(500, new { message = "Error al enviar notificación multi-canal" });
        }
    }

}

