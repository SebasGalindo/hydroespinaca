using Microsoft.AspNetCore.Mvc;
using SensorService.Application.DTOs;
using SensorService.Application.Interfaces;

namespace SensorService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AggregatesController : ControllerBase
{
    private readonly IAggregateService _service;

    public AggregatesController(IAggregateService service)
    {
        _service = service;
    }

    [HttpGet("{sensorId}/{variableId}")]
    public async Task<IActionResult> GetBySensorAndVariable(string sensorId, string variableId, [FromQuery] DateTime from, [FromQuery] DateTime to)
    {
        var aggregates = await _service.GetBySensorAndVariableAsync(sensorId, variableId, from, to);
        return Ok(aggregates);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] AggregateDto dto)
    {
        await _service.SaveAsync(dto);
        return Ok();
    }
}
