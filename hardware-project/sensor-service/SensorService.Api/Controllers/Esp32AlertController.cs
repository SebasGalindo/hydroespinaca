using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using HydroEspinaca.Shared.Extensions;
using SensorService.Application.DTOs.Alert;
using SensorService.Application.Interfaces;

namespace SensorService.Api.Controllers;

[ApiController]
[Route("api/esp32-alerts")]
public class Esp32AlertController : ControllerBase
{
    private readonly IEsp32AlertService _service;

    public Esp32AlertController(IEsp32AlertService service)
    {
        _service = service;
    }

    [HttpGet("esp32/{esp32Id}")]
    [Authorize(Policy = PolicyNames.Esp32AlertRead)]
    public async Task<IActionResult> GetByEsp32Id(string esp32Id)
    {
        var alerts = await _service.GetByEsp32IdAsync(esp32Id);
        return Ok(alerts);
    }

    [HttpPatch("{alertId}/acknowledge")]
    [Authorize(Policy = PolicyNames.Esp32AlertWrite)]
    public async Task<IActionResult> Acknowledge(string alertId, [FromBody] Esp32AlertUpdateDto dto)
    {
        await _service.AcknowledgeAsync(alertId, dto);
        return NoContent();
    }
}
