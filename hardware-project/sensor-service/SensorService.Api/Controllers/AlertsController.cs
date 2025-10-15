using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using HydroEspinaca.Shared.Extensions;
using SensorService.Application.DTOs.Alert;
using SensorService.Application.Interfaces;
using SensorService.Domain.Interfaces;

namespace SensorService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AlertsController : ControllerBase
{
    private readonly ISensorAlertService _service;
    private readonly ICriticalAlertNotificationService _notificationService;

    public AlertsController(ISensorAlertService service, ICriticalAlertNotificationService notificationService)
    {
        _service = service;
        _notificationService = notificationService;
    }

    [HttpGet("sensor/{sensorId}")]
    [Authorize(Policy = PolicyNames.AlertRead)]
    public async Task<IActionResult> GetBySensorId(string sensorId)
    {
        var alerts = await _service.GetBySensorIdAsync(sensorId);
        return Ok(alerts);
    }

    [HttpPatch("{alertId}/acknowledge")]
    [Authorize(Policy = PolicyNames.AlertWrite)]
    public async Task<IActionResult> Acknowledge(string alertId, [FromBody] SensorAlertUpdateDto dto)
    {
        await _service.AcknowledgeAsync(alertId, dto);
        return NoContent();
    }

    /// <summary>
    /// Clear all in-memory notification caches.
    /// Use this when manually deleting alerts collection for testing/debugging.
    /// </summary>
    [HttpPost("clear-notification-cache")]
    [Authorize(Policy = PolicyNames.AlertWrite)]
    public IActionResult ClearNotificationCache()
    {
        _notificationService.ClearAllCaches();
        return Ok(new { message = "Notification caches cleared successfully" });
    }

}
