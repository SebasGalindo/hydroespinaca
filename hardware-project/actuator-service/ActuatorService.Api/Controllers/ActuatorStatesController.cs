using ActuatorService.Domain.Interfaces;
using HydroEspinaca.Shared.Errors;
using HydroEspinaca.Shared.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ActuatorService.Api.Controllers;

[ApiController]
[Route("api/actuators")]
public class ActuatorStatesController : ControllerBase
{
    private readonly IActuatorStateMachine _stateMachine;
    private readonly ILogger<ActuatorStatesController> _logger;

    public ActuatorStatesController(
        IActuatorStateMachine stateMachine,
        ILogger<ActuatorStatesController> logger)
    {
        _stateMachine = stateMachine;
        _logger = logger;
    }

    /// <summary>
    /// Gets all current actuator states from the in-memory state machine.
    /// </summary>
    [HttpGet("states")]
    [Authorize(Policy = PolicyNames.ActuatorRead)]
    public ActionResult GetAllStates()
    {
        var states = _stateMachine.GetAllStates();

        _logger.LogDebug("📊 Retrieved {Count} actuator states", states.Count);

        return Ok(new
        {
            count = states.Count,
            states = states.Select(kvp => new
            {
                actuatorId = kvp.Key,
                esp32Id = kvp.Value.Esp32Id,
                pin = kvp.Value.Pin,
                mode = kvp.Value.Mode.ToString(),
                state = kvp.Value.State.ToString(),
                remainingDuration = kvp.Value.RemainingDuration,
                dutyCycle = kvp.Value.DutyCycle,
                lastCommandId = kvp.Value.LastCommandId,
                lastUpdated = kvp.Value.LastUpdated
            })
        });
    }

    /// <summary>
    /// Gets the current state of a specific actuator.
    /// </summary>
    [HttpGet("{actuatorId}/state")]
    [Authorize(Policy = PolicyNames.ActuatorRead)]
    public ActionResult GetState(string actuatorId)
    {
        var state = _stateMachine.GetState(actuatorId);

        if (state == null)
        {
            _logger.LogWarning("⚠️ State not found for actuator {ActuatorId}", actuatorId);
            return NotFound(new
            {
                error = "State not found",
                actuatorId
            });
        }

        _logger.LogDebug("📊 Retrieved state for actuator {ActuatorId}", actuatorId);

        return Ok(new
        {
            actuatorId = state.ActuatorId,
            esp32Id = state.Esp32Id,
            pin = state.Pin,
            mode = state.Mode.ToString(),
            state = state.State.ToString(),
            remainingDuration = state.RemainingDuration,
            dutyCycle = state.DutyCycle,
            lastCommandId = state.LastCommandId,
            lastUpdated = state.LastUpdated
        });
    }
}
