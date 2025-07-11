using Microsoft.AspNetCore.Mvc;
using SensorService.Application.DTOs;
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

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ReadingDto dto)
    {
        await _service.AddAsync(dto);
        return Ok();
    }

    [HttpGet("{sensorId}/{variableId}")]
    public async Task<IActionResult> GetBySensorAndVariable(string sensorId, string variableId, [FromQuery] DateTime from, [FromQuery] DateTime to)
    {
        var result = await _service.GetBySensorAndVariableAsync(sensorId, variableId, from, to);
        return Ok(result);
    }
}