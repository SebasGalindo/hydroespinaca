using System.Collections.Concurrent;
using ActuatorService.Domain.Interfaces;
using ActuatorService.Domain.Models;
using HydroEspinaca.Shared.Enums;
using Microsoft.Extensions.Logging;

namespace ActuatorService.Application.Services;

/// <summary>
/// In-memory state machine for tracking actuator runtime states.
/// Thread-safe implementation using ConcurrentDictionary.
/// </summary>
public class ActuatorStateMachine : IActuatorStateMachine
{
    private readonly ConcurrentDictionary<string, ActuatorState> _states = new();
    private readonly ILogger<ActuatorStateMachine> _logger;

    public ActuatorStateMachine(ILogger<ActuatorStateMachine> logger)
    {
        _logger = logger;
    }

    public ActuatorState? GetState(string actuatorId)
    {
        _states.TryGetValue(actuatorId, out var state);
        return state;
    }

    public IReadOnlyDictionary<string, ActuatorState> GetAllStates()
    {
        return _states.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }

    public void UpdateState(string actuatorId, PowerState newState, double? duration = null, double? dutyCycle = null, string? commandId = null)
    {
        var state = _states.GetOrAdd(actuatorId, _ =>
        {
            _logger.LogWarning("⚠️ Actuator {ActuatorId} not initialized, creating default state", actuatorId);
            return new ActuatorState { ActuatorId = actuatorId };
        });

        state.State = newState;
        state.RemainingDuration = duration;
        state.DutyCycle = dutyCycle;
        state.LastCommandId = commandId;
        state.LastUpdated = DateTime.UtcNow;

        _logger.LogDebug("🔄 Updated state for actuator {ActuatorId}: {State}, Duration: {Duration}s, DutyCycle: {DutyCycle}%, Command: {CommandId}",
            actuatorId, newState, duration, dutyCycle, commandId);
    }

    public void InitializeActuator(string actuatorId, string esp32Id, string pin, ActuatorMode mode)
    {
        var state = _states.AddOrUpdate(
            actuatorId,
            _ => new ActuatorState
            {
                ActuatorId = actuatorId,
                Esp32Id = esp32Id,
                Pin = pin,
                Mode = mode,
                State = PowerState.OFF,
                LastUpdated = DateTime.UtcNow
            },
            (_, existingState) =>
            {
                existingState.Esp32Id = esp32Id;
                existingState.Pin = pin;
                existingState.Mode = mode;
                existingState.LastUpdated = DateTime.UtcNow;
                return existingState;
            }
        );

        _logger.LogDebug("✅ Initialized actuator {ActuatorId} on ESP32 {Esp32Id}, Pin {Pin}, Mode {Mode}",
            actuatorId, esp32Id, pin, mode);
    }

    public void ResetAll()
    {
        var count = _states.Count;
        _states.Clear();
        _logger.LogInformation("🧹 Reset all actuator states. Cleared {Count} states", count);
    }
}
