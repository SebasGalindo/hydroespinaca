using Microsoft.AspNetCore.Mvc;
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

    [HttpPost("acknowledge/{alertId}")]
    public async Task<IActionResult> Acknowledge(string alertId, bool acknowledged)
    {
        await _service.AcknowledgeAsync(alertId, acknowledged);
        return NoContent();
    }
}
