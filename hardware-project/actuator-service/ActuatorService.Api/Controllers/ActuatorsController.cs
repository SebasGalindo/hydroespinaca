using ActuatorService.Application.Interfaces;
using HydroEspinaca.Shared.DTOs.Actuator;
using HydroEspinaca.Shared.Errors;
using HydroEspinaca.Shared.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ActuatorService.Api.Controllers;

/// <summary>
/// API controller for CRUD operations on actuator devices and usage analytics.
/// </summary>
[ApiController]
[Route("api/actuators")]
public class ActuatorsController : ControllerBase
{
    private readonly IActuatorService _service;

    public ActuatorsController(IActuatorService service)
    {
        _service = service;
    }

    [HttpGet]
    [Authorize(Policy = PolicyNames.ActuatorRead)]
    public async Task<ActionResult<List<ActuatorDto>>> GetAll()
    {
        var result = await _service.GetAllAsync();
        return Ok(result);
    }


    [HttpGet("{id}")]
    [Authorize(Policy = PolicyNames.ActuatorRead)]
    public async Task<ActionResult<ActuatorDto>> GetById(string id)
    {
        var result = await _service.GetByIdAsync(id);
        return Ok(result);
    }



    [HttpGet("esp32/{esp32Id}")]
    [Authorize(Policy = PolicyNames.ActuatorRead)]
    public async Task<ActionResult<List<ActuatorDto>>> GetByEsp32Id(string esp32Id)
    {
        var result = await _service.GetByEsp32IdAsync(esp32Id);
        return Ok(result);
    }


    [HttpPost]
    [Authorize(Policy = PolicyNames.ActuatorCreate)]
    public async Task<ActionResult<string>> Create([FromBody] CreateActuatorDto dto)
    {
        var id = await _service.AddAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id }, id);
    }


    [HttpPut("{id}")]
    [Authorize(Policy = PolicyNames.ActuatorUpdate)]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateActuatorDto dto)
    {
        await _service.UpdateAsync(id, dto);
        return NoContent();
    }


    [HttpDelete("{id}")]
    [Authorize(Policy = PolicyNames.ActuatorDelete)]
    public async Task<IActionResult> Delete(string id)
    {
        await _service.DeleteAsync(id);
        return NoContent();
    }

}
