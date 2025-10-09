using ActuatorService.Application.Interfaces;
using HydroEspinaca.Shared.DTOs.Actuator;
using HydroEspinaca.Shared.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ActuatorService.Api.Controllers;

[ApiController]
[Route("api/commands")]
public class CommandsController : ControllerBase
{
    private readonly IExecuteMultiRoutineCommandUseCase _executeMultiRoutineCommandUseCase;
    private readonly IJobScheduleService _jobScheduleService;
    private readonly IRoutineCommandService _routineCommandService;

    public CommandsController(
        IExecuteMultiRoutineCommandUseCase executeMultiRoutineCommandUseCase,
        IJobScheduleService jobScheduleService,
        IRoutineCommandService routineCommandService)
    {
        _executeMultiRoutineCommandUseCase = executeMultiRoutineCommandUseCase;
        _jobScheduleService = jobScheduleService;
        _routineCommandService = routineCommandService;
    }

    [HttpPost]
    [Authorize(Policy = PolicyNames.CommandCreate)]
    public async Task<IActionResult> ExecuteRoutines([FromBody] List<RoutineCommandDto> routines)
    {
        var commandIds = await _executeMultiRoutineCommandUseCase.ExecuteAsync(
            new MultiRoutineCommandDto { Routines = routines }
        );

        return Ok(new { CommandIds = commandIds });
    }

    [HttpGet("jobs/status")]
    [Authorize(Policy = PolicyNames.CommandRead)]
    public async Task<IActionResult> GetJobsStatus([FromQuery] string? esp32Id = null)
    {
        var jobStatus = await _jobScheduleService.GetJobStatusAsync(esp32Id);
        return Ok(jobStatus);
    }

    [HttpGet("routines")]
    [Authorize(Policy = PolicyNames.CommandRead)]
    public async Task<IActionResult> GetRoutineCommands([FromQuery] string? esp32Id = null)
    {
        var routineCommands = await _routineCommandService.GetAllRoutineCommandsAsync(esp32Id);
        return Ok(routineCommands);
    }

    [HttpGet("routines/{commandId}")]
    [Authorize(Policy = PolicyNames.CommandRead)]
    public async Task<IActionResult> GetRoutineCommand(string commandId)
    {
        var routineCommand = await _routineCommandService.GetRoutineCommandByIdAsync(commandId);
        return Ok(routineCommand);
    }

    [HttpDelete("jobs/clear")]
    [Authorize(Policy = PolicyNames.ActuatorControl)]
    public async Task<IActionResult> ClearJobSchedule([FromQuery] string? esp32Id = null)
    {
        await _jobScheduleService.ClearJobScheduleAsync(esp32Id);
        return Ok(new { Message = esp32Id != null 
            ? $"Job schedule cleared for ESP32: {esp32Id}" 
            : "All job schedules cleared" });
    }
}
