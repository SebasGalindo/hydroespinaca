using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using HydroEspinaca.Shared.Extensions;
using SensorService.Application.DTOs.Reading;
using SensorService.Application.Interfaces;

namespace SensorService.Api.Controllers;

/// <summary>
/// API controller for querying historical sensor readings.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ReadingsController : ControllerBase
{
    private readonly IReadingService _service;
    private readonly ILatestReadingsService _latestReadingsService;

    public ReadingsController(IReadingService service, ILatestReadingsService latestReadingsService)
    {
        _service = service;
        _latestReadingsService = latestReadingsService;
    }

    [HttpGet("{sensorId}/{variableId}")]
    [Authorize(Policy = PolicyNames.ReadingRead)]
    public async Task<IActionResult> GetBySensorAndVariable(string sensorId, string variableId, [FromQuery] DateTime from, [FromQuery] DateTime to)
    {
        var result = await _service.GetBySensorAndVariableAsync(sensorId, variableId, from, to);
        return Ok(result);
    }

    [HttpGet("latest")]
    [Authorize(Policy = PolicyNames.ReadingRead)]
    public async Task<IActionResult> GetLatestReadings()
    {
        var result = await _latestReadingsService.GetEnrichedLatestReadingsAsync();

        if (result is null)
            return NotFound("No latest readings available");

        return Ok(result);
    }
}