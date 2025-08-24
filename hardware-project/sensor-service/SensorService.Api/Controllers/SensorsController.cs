using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using HydroEspinaca.Shared.DTOs.Sensors;
using HydroEspinaca.Shared.Extensions;
using SensorService.Application.Interfaces;

namespace SensorService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SensorsController : ControllerBase
{
    private readonly ISensorService _service;

    public SensorsController(ISensorService service)
    {
        _service = service;
    }

    [HttpGet]
    [Authorize(Policy = PolicyNames.SensorRead)]
    public async Task<IActionResult> GetAll() => Ok(await _service.GetAllAsync());

    [HttpGet("{id}")]
    [Authorize(Policy = PolicyNames.SensorRead)]
    public async Task<IActionResult> GetById(string id)
    {
        var sensor = await _service.GetByIdAsync(id);
        return sensor is null ? NotFound() : Ok(sensor);
    }

    [HttpPost]
    [Authorize(Policy = PolicyNames.SensorCreate)]
    public async Task<IActionResult> Create([FromBody] SensorCreateDto dto)
    {
        var id = await _service.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id }, new { id });
    }

    [HttpPut("{id}")]
    [Authorize(Policy = PolicyNames.SensorUpdate)]
    public async Task<IActionResult> Update(string id, [FromBody] SensorUpdateDto dto)
    {
        await _service.UpdateAsync(id, dto);
        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = PolicyNames.SensorDelete)]
    public async Task<IActionResult> Delete(string id)
    {
        await _service.DeleteAsync(id);
        return NoContent();
    }
}
