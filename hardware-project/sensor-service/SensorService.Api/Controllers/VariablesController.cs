using Microsoft.AspNetCore.Mvc;
using SensorService.Application.DTOs.Variable;
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
    public async Task<IActionResult> Create([FromBody] VariableCreateDto dto)
    {
        await _service.AddAsync(dto);
        return Ok();
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] VariableUpdateDto dto)
    {
        await _service.UpdateAsync(id, dto);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        await _service.DeleteAsync(id);
        return NoContent();
    }
}
