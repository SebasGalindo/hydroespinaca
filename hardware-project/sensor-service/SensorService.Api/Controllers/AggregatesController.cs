using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using HydroEspinaca.Shared.Extensions;
using SensorService.Application.Interfaces;
using HydroEspinaca.Shared.DTOs.Analytics;
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
    [Authorize(Policy = PolicyNames.AggregateRead)]
    public async Task<IActionResult> GetBySensorAndVariable(string sensorId, string variableId, [FromQuery] DateTime from, [FromQuery] DateTime to)
    {
        var aggregates = await _service.GetBySensorAndVariableAsync(sensorId, variableId, from, to);
        return Ok(aggregates);
    }

    [HttpPost("environmental")]
    [Authorize(Policy = PolicyNames.AggregateRead)]
    public async Task<IActionResult> GetEnvironmentalAggregates([FromBody] EnvironmentalAnalyticsRequest request)
    {
        var aggregates = await _service.GetEnvironmentalAggregatesAsync(request);
        return Ok(aggregates);
    }
}
