using HydroEspinaca.Shared.DTOs.Actuator;
using ActuatorService.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ActuatorService.Api.Controllers;

[ApiController]
[Route("api/commands")]
public class CommandsController : ControllerBase
{
    private readonly ICommandService _commandService;

    public CommandsController(ICommandService commandService)
    {
        _commandService = commandService;
    }

    [HttpPost]
    public async Task<IActionResult> Register([FromBody] CreateCommandDto dto)
    {
        string? userId = HttpContext.User?.Identity?.Name;
        await _commandService.AddAsync(dto, userId);
        return Ok();
    }

    [HttpGet("actuator/{actuatorId}")]
    public async Task<ActionResult<List<ActuatorCommandDto>>> GetByActuatorId(string actuatorId)
    {
        var result = await _commandService.GetByActuatorIdAsync(actuatorId);
        return Ok(result);
    }

    [HttpGet("range")]
    public async Task<ActionResult<List<ActuatorCommandDto>>> GetByDateRange([FromQuery] DateTime from, [FromQuery] DateTime to)
    {
        var result = await _commandService.GetByDateRangeAsync(from, to);
        return Ok(result);
    }
}
