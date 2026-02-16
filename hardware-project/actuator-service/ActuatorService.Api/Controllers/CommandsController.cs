using ActuatorService.Application.DTOs;
using ActuatorService.Application.Interfaces;
using ActuatorService.Application.Services;
using ActuatorService.Application.UseCases;
using ActuatorService.Domain.Interfaces;
using HydroEspinaca.Shared.DTOs.Actuator;
using HydroEspinaca.Shared.DTOs.Analytics;
using HydroEspinaca.Shared.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ActuatorService.Api.Controllers;

/// <summary>
/// API controller for executing and tracking actuator routine commands.
/// </summary>
[ApiController]
[Route("api/commands")]
public class CommandsController : ControllerBase
{
    private readonly IExecuteCommandsUseCase _executeCommandsUseCase;
    private readonly ICommandExecutionService _commandExecutionService;
    private readonly IInternalRoutineRepository _internalRoutineRepository;
    private readonly IGetActuatorAnalyticsUseCase _getActuatorAnalyticsUseCase;

    public CommandsController(
        IExecuteCommandsUseCase executeCommandsUseCase,
        ICommandExecutionService commandExecutionService,
        IInternalRoutineRepository internalRoutineRepository,
        IGetActuatorAnalyticsUseCase getActuatorAnalyticsUseCase)
    {
        _executeCommandsUseCase = executeCommandsUseCase;
        _commandExecutionService = commandExecutionService;
        _internalRoutineRepository = internalRoutineRepository;
        _getActuatorAnalyticsUseCase = getActuatorAnalyticsUseCase;
    }

    [HttpPost("execute")]
    [Authorize(Policy = PolicyNames.CommandCreate)]
    public async Task<IActionResult> ExecuteCommands([FromBody] ExecuteCommandsDto executeCommands)
    {
        var commandIds = await _executeCommandsUseCase.ExecuteAsync(executeCommands);
        return Ok(new { CommandIds = commandIds });
    }

    [HttpGet("jobs/status")]
    [Authorize(Policy = PolicyNames.CommandRead)]
    public async Task<IActionResult> GetJobsStatus([FromQuery] string? esp32Id = null)
    {
        var jobStatus = await _commandExecutionService.GetStatusAsync(esp32Id);
        var stats = await _commandExecutionService.GetStatsAsync();

        var internalRoutines = await _internalRoutineRepository.GetActiveRoutinesAsync();

        var internalRoutinesInfo = internalRoutines.Select(r => new
        {
            r.Name,
            r.Description,
            Interval = r.Interval.ToString(@"hh\:mm\:ss"),
            NextExecutionEstimate = r.IsActive
                ? InternalRoutineScheduler.GetNextExecution(r.Interval, "America/Bogota")
                : (DateTime?)null,
            r.IsActive
        }).ToList();

        return Ok(new
        {
            JobStatus = jobStatus,
            Stats = stats,
            InternalRoutines = internalRoutinesInfo
        });
    }

    [HttpDelete("jobs/clear")]
    [Authorize(Policy = PolicyNames.ActuatorControl)]
    public async Task<IActionResult> ClearJobSchedule([FromQuery] string? esp32Id = null)
    {
        await _commandExecutionService.ClearAsync(esp32Id);
        await _commandExecutionService.ResetAllActuatorsAsync(esp32Id);

        return Ok(new { Message = esp32Id != null
            ? $"Job schedule cleared and all actuators reset for ESP32: {esp32Id}"
            : "All job schedules cleared and all actuators reset" });
    }

    [HttpPost("analytics")]
    [Authorize(Policy = PolicyNames.CommandRead)]
    public async Task<IActionResult> GetActuatorAnalytics([FromBody] ActuatorAnalyticsRequest request)
    {
        var result = await _getActuatorAnalyticsUseCase.ExecuteAsync(request);
        return Ok(result);
    }
}
