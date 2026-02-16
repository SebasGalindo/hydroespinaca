using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using HydroEspinaca.Shared.DTOs.Variables;
using HydroEspinaca.Shared.Extensions;
using SensorService.Application.Interfaces;

namespace SensorService.Api.Controllers;

/// <summary>
/// API controller for CRUD operations on environmental variable definitions.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class VariablesController : ControllerBase
{
    private readonly IVariableService _service;

    public VariablesController(IVariableService service)
    {
        _service = service;
    }

    [HttpGet]
    [Authorize(Policy = PolicyNames.VariableRead)]
    public async Task<IActionResult> GetAll() => Ok(await _service.GetAllAsync());

    [HttpGet("{id}")]
    [Authorize(Policy = PolicyNames.VariableRead)]
    public async Task<IActionResult> GetById(string id)
    {
        var variable = await _service.GetByIdAsync(id);
        return variable is null ? NotFound() : Ok(variable);
    }

    [HttpPost]
    [Authorize(Policy = PolicyNames.VariableWrite)]
    public async Task<IActionResult> Create([FromBody] VariableCreateDto dto)
    {
        await _service.AddAsync(dto);
        return Ok();
    }

    [HttpPut("{id}")]
    [Authorize(Policy = PolicyNames.VariableWrite)]
    public async Task<IActionResult> Update(string id, [FromBody] VariableUpdateDto dto)
    {
        await _service.UpdateAsync(id, dto);
        return NoContent();
    }


    [HttpDelete("{id}")]
    [Authorize(Policy = PolicyNames.VariableWrite)]
    public async Task<IActionResult> Delete(string id)
    {
        await _service.DeleteAsync(id);
        return NoContent();
    }
}
