using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using HydroEspinaca.Shared.Extensions;
using SensorService.Application.DTOs.Reading;
using SensorService.Application.Interfaces;

namespace SensorService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReadingsController : ControllerBase
{
    private readonly IReadingService _service;

    public ReadingsController(IReadingService service)
    {
        _service = service;
    }

    [HttpGet("{sensorId}/{variableId}")]
    [Authorize(Policy = PolicyNames.ReadingRead)]
    public async Task<IActionResult> GetBySensorAndVariable(string sensorId, string variableId, [FromQuery] DateTime from, [FromQuery] DateTime to)
    {
        var result = await _service.GetBySensorAndVariableAsync(sensorId, variableId, from, to);
        return Ok(result);
    }
}