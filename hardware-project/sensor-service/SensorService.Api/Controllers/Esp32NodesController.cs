using HydroEspinaca.Shared.DTOs.Esp32;
using HydroEspinaca.Shared.Enums;
using HydroEspinaca.Shared.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SensorService.Application.Interfaces;
using SensorService.Application.Services;

namespace SensorService.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class Esp32NodesController : ControllerBase
{
    private readonly IEsp32NodeService _service;

    public Esp32NodesController(IEsp32NodeService service)
    {
        _service = service;
    }

    [HttpGet]
    [Authorize(Policy = PolicyNames.Esp32Read)]
    public async Task<IActionResult> GetAll()
    {
        var nodes = await _service.GetAllAsync();
        return Ok(nodes);
    }

    [HttpGet("{id}")]
    [Authorize(Policy = PolicyNames.Esp32Read)]
    public async Task<IActionResult> GetById(string id)
    {
        var node = await _service.GetByIdAsync(id);
        return node is null ? NotFound() : Ok(node);
    }

    [HttpPost]
    [Authorize(Policy = PolicyNames.Esp32Write)]
    public async Task<IActionResult> Create([FromBody] Esp32NodeCreateDto dto)
    {
        var created = await _service.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = created }, created);
    }
    
    [HttpPut("{id}/status")]
    [Authorize(Policy = PolicyNames.Esp32Write)]
    public async Task<IActionResult> UpdateStatus(string id, [FromBody] Esp32NodeUpdateStatusDto dto)
    {
        await _service.UpdateStatusAsync(id, dto);
        return NoContent();
    }

    [HttpGet("{id}/exists")]
    [Authorize(Policy = PolicyNames.Esp32Read)]
    public async Task<IActionResult> Exists(string id)
    {
        var exists = await _service.ExistsAsync(id);
        return Ok(new { exists });
    }

}
