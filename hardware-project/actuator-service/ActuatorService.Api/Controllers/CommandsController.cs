using ActuatorService.Application.DTOs;
using ActuatorService.Application.Interfaces;
using ActuatorService.Domain.Interfaces;
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
    private readonly IRoutineExecutionService _routineExecutionService;
    private readonly IRoutineCommandService _routineCommandService;
    private readonly IInternalRoutineRepository _internalRoutineRepository;

    public CommandsController(
        IExecuteMultiRoutineCommandUseCase executeMultiRoutineCommandUseCase,
        IRoutineExecutionService routineExecutionService,
        IRoutineCommandService routineCommandService,
        IInternalRoutineRepository internalRoutineRepository)
    {
        _executeMultiRoutineCommandUseCase = executeMultiRoutineCommandUseCase;
        _routineExecutionService = routineExecutionService;
        _routineCommandService = routineCommandService;
        _internalRoutineRepository = internalRoutineRepository;
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
        var jobStatus = await _routineExecutionService.GetStatusAsync(esp32Id);
        var stats = await _routineExecutionService.GetStatsAsync();

        // Get internal routines info
        var internalRoutines = await _internalRoutineRepository.GetActiveRoutinesAsync();
        var now = DateTime.UtcNow;

        var internalRoutinesInfo = internalRoutines.Select(r => new
        {
            r.Name,
            r.Description,
            Interval = r.Interval.ToString(@"hh\:mm\:ss"),
            r.LastExecutedAt,
            NextExecutionEstimate = CalculateNextExecution(r, now),
            r.IsActive
        }).ToList();

        return Ok(new
        {
            JobStatus = jobStatus,
            Stats = stats,
            InternalRoutines = internalRoutinesInfo
        });
    }

    private DateTime? CalculateNextExecution(Domain.Entities.InternalRoutine routine, DateTime now)
    {
        if (!routine.IsActive)
            return null;

        if (routine.LastExecutedAt == null)
        {
            var todayStart = now.Date + routine.StartTime;
            return todayStart > now ? todayStart : todayStart.Add(routine.Interval);
        }

        return routine.LastExecutedAt.Value.Add(routine.Interval);
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
        await _routineExecutionService.ClearAsync(esp32Id);
        return Ok(new { Message = esp32Id != null
            ? $"Job schedule cleared for ESP32: {esp32Id}"
            : "All job schedules cleared" });
    }
}
