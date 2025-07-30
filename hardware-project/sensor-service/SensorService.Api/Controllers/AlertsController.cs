using Microsoft.AspNetCore.Mvc;
using SensorService.Application.DTOs.Alert;
using SensorService.Application.Interfaces;

namespace SensorService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AlertsController : ControllerBase
{
    private readonly ISensorAlertService _service;

    public AlertsController(ISensorAlertService service)
    {
        _service = service;
    }

    [HttpGet("sensor/{sensorId}")]
    public async Task<IActionResult> GetBySensorId(string sensorId)
    {
        var alerts = await _service.GetBySensorIdAsync(sensorId);
        return Ok(alerts);
    }

    [HttpPatch("{alertId}/acknowledge")]
    public async Task<IActionResult> Acknowledge(string alertId, [FromBody] SensorAlertUpdateDto dto)
    {
        await _service.AcknowledgeAsync(alertId, dto);
        return NoContent();
    }

}
