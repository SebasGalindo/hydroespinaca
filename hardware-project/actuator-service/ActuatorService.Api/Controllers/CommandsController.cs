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

    public CommandsController(
        IExecuteMultiRoutineCommandUseCase executeMultiRoutineCommandUseCase,
        IJobScheduleService jobScheduleService)
    {
        _executeMultiRoutineCommandUseCase = executeMultiRoutineCommandUseCase;
        _jobScheduleService = jobScheduleService;
    }

    [HttpPost]
    [Authorize(Policy = PolicyNames.CommandCreate)]
    public async Task<IActionResult> ExecuteRoutines([FromBody] List<RoutineCommandDto> routines)
    {
        var multiRoutineCommand = new MultiRoutineCommandDto { Routines = routines };
        var commandIds = await _executeMultiRoutineCommandUseCase.ExecuteAsync(multiRoutineCommand);
        return Ok(new { CommandIds = commandIds });
    }

    [HttpGet("jobs/status")]
    [Authorize(Policy = PolicyNames.CommandRead)]
    public async Task<IActionResult> GetJobsStatus([FromQuery] string? esp32Id = null)
    {
        var jobStatus = await _jobScheduleService.GetJobStatusAsync(esp32Id);
        return Ok(jobStatus);
    }
}
