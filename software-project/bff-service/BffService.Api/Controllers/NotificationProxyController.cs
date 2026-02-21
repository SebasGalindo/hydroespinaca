using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using BffService.Application.Interfaces;
using BffService.Domain.DTOs;

namespace BffService.Api.Controllers;

/// <summary>
/// Proxies notification-service endpoints for preferences, push subscriptions and history.
/// </summary>
[ApiController]
[Route("notifications")]
[Authorize]
public class NotificationProxyController : ControllerBase
{
    private readonly INotificationServiceClient _client;
    private readonly ILogger<NotificationProxyController> _logger;

    public NotificationProxyController(
        INotificationServiceClient client,
        ILogger<NotificationProxyController> logger)
    {
        _client = client;
        _logger = logger;
    }

    // ──────────────── Preferences ────────────────

    /// <summary>
    /// Gets notification preferences for a user.
    /// </summary>
    [HttpGet("preferences/{userId}")]
    public async Task<IActionResult> GetPreferences(string userId, CancellationToken ct)
    {
        try
        {
            var result = await _client.GetPreferencesAsync(userId, ct);
            if (result == null) return NotFound(new { message = $"No preferences found for user '{userId}'" });
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting notification preferences for {UserId}", userId);
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
            var result = await _client.UpdatePreferencesAsync(userId, request, ct);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating notification preferences for {UserId}", userId);
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
            var result = await _client.RegisterPushAsync(request, ct);
            return Created($"notifications/push/subscriptions/{request.UserId}", result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering push subscription");
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
            await _client.UnregisterPushAsync(subscriptionId, ct);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unregistering push subscription {SubscriptionId}", subscriptionId);
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
            var result = await _client.GetPushSubscriptionsAsync(userId, platform, ct);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting push subscriptions for {UserId}", userId);
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
            var result = await _client.GetNotificationHistoryAsync(userId, channel, from, to, limit, ct);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting notification history for {UserId}", userId);
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
            var result = await _client.SendMultiChannelAsync(request, ct);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending multi-channel notification");
            return StatusCode(500, new { message = "Error al enviar notificación multi-canal" });
        }
    }
}
