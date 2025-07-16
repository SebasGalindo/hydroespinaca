using Microsoft.AspNetCore.Mvc;
using SensorService.Application.DTOs.Esp32Node;
using SensorService.Application.Interfaces;

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
    public async Task<IActionResult> GetAll()
    {
        var nodes = await _service.GetAllAsync();
        return Ok(nodes);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var node = await _service.GetByIdAsync(id);
        return node is null ? NotFound() : Ok(node);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Esp32NodeCreateDto dto)
    {
        await _service.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto);
    }

    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateStatus(string id, [FromBody] Esp32NodeUpdateStatusDto status)
    {
        await _service.UpdateStatusAsync(id, status);
        return NoContent();
    }
}
