using Microsoft.AspNetCore.Mvc;
using SensorService.Application.DTOs;
using SensorService.Application.Interfaces;

namespace SensorService.Api.Controllers;

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
    public async Task<IActionResult> GetAll() => Ok(await _service.GetAllAsync());

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var variable = await _service.GetByIdAsync(id);
        return variable is null ? NotFound() : Ok(variable);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] VariableDto dto)
    {
        await _service.AddAsync(dto);
        return Ok();
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] VariableDto dto)
    {
        if (id != dto.Id)
            return BadRequest("ID in URL does not match ID in body.");

        await _service.UpdateAsync(dto);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        await _service.DeleteAsync(id);
        return NoContent();
    }
}
