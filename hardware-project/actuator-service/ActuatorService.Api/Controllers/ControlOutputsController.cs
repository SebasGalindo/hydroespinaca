using ActuatorService.Application.Interfaces;
using HydroEspinaca.Shared.DTOs.Actuator;
using HydroEspinaca.Shared.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ActuatorService.Api.Controllers;

[ApiController]
[Route("api/outputs")]
public class ControlOutputsController : ControllerBase
{
    private readonly IControlOutputService _service;

    public ControlOutputsController(IControlOutputService service)
    {
        _service = service;
    }

    /// <summary>
    /// Obtiene todos los control outputs
    /// </summary>
    [HttpGet]
    [Authorize(Policy = PolicyNames.ActuatorRead)]
    public async Task<ActionResult<List<ControlOutputDto>>> GetAll()
    {
        var result = await _service.GetAllAsync();
        return Ok(result);
    }

    /// <summary>
    /// Obtiene un control output por su ID
    /// </summary>
    [HttpGet("{id}")]
    [Authorize(Policy = PolicyNames.ActuatorRead)]
    public async Task<ActionResult<ControlOutputDto>> GetById(string id)
    {
        var result = await _service.GetByIdAsync(id);
        return Ok(result);
    }

    /// <summary>
    /// Obtiene todos los control outputs de un actuador específico
    /// </summary>
    [HttpGet("actuator/{actuatorId}")]
    [Authorize(Policy = PolicyNames.ActuatorRead)]
    public async Task<ActionResult<List<ControlOutputDto>>> GetByActuatorId(string actuatorId)
    {
        var result = await _service.GetByActuatorIdAsync(actuatorId);
        return Ok(result);
    }

    /// <summary>
    /// Crea un nuevo control output
    /// </summary>
    [HttpPost]
    [Authorize(Policy = PolicyNames.ActuatorCreate)]
    public async Task<ActionResult<string>> Create([FromBody] CreateControlOutputDto dto)
    {
        var id = await _service.AddAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id }, id);
    }

    /// <summary>
    /// Actualiza un control output existente
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Policy = PolicyNames.ActuatorUpdate)]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateControlOutputDto dto)
    {
        await _service.UpdateAsync(id, dto);
        return NoContent();
    }

    /// <summary>
    /// Elimina un control output
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Policy = PolicyNames.ActuatorDelete)]
    public async Task<IActionResult> Delete(string id)
    {
        await _service.DeleteAsync(id);
        return NoContent();
    }
}
