using ActuatorService.Application.DTOs;
using ActuatorService.Application.Interfaces;
using ActuatorService.Domain.Exceptions;
using HydroEspinaca.Shared.Constants;
using HydroEspinaca.Shared.DTOs.Actuator;
using HydroEspinaca.Shared.Enums;

namespace ActuatorService.Application.Services;

public class RoutineValidationService : IRoutineValidationService
{
    private readonly IOutputVariableResolver _outputVariableResolver;

    public RoutineValidationService(IOutputVariableResolver outputVariableResolver)
    {
        _outputVariableResolver = outputVariableResolver;
    }

    public async Task<List<ResolvedRoutineStepDto>> ValidateAndResolveStepsAsync(List<RoutineStepDto> steps)
    {
        var resolvedSteps = new List<ResolvedRoutineStepDto>();

        foreach (var step in steps)
        {
            // Resolve outputVariable to physical actuator
            var (controlOutput, actuator) = await _outputVariableResolver.ResolveAsync(step.OutputVariable);

            // Validate step parameters for actuator mode
            ValidateStepForMode(step, actuator.Mode, controlOutput.Name);

            // Create resolved step with physical data
            var resolvedStep = new ResolvedRoutineStepDto
            {
                OutputVariableId = controlOutput.Id,
                OutputVariableName = controlOutput.Name,
                ActuatorId = actuator.Id,
                Esp32Id = actuator.Esp32Id,
                Pin = actuator.Pin,
                Mode = actuator.Mode,
                Power = step.Power,
                DutyCycle = step.DutyCycle,
                Duration = step.Duration
            };

            resolvedSteps.Add(resolvedStep);
        }

        return resolvedSteps;
    }

    private void ValidateStepForMode(RoutineStepDto step, ActuatorMode mode, string outputVariableName)
    {
        switch (mode)
        {
            case ActuatorMode.DIGITAL:
                if (step.DutyCycle.HasValue)
                {
                    throw new RoutineScheduleConflictException($"Digital output '{outputVariableName}' cannot use 'dutyCycle'. Use 'power' instead.");
                }
                if (string.IsNullOrEmpty(step.Power))
                {
                    throw new RoutineScheduleConflictException($"Digital output '{outputVariableName}' requires 'power' parameter ({ActuatorConstants.PowerStates.On}/{ActuatorConstants.PowerStates.Off}).");
                }
                break;

            case ActuatorMode.PWM:
                if (!string.IsNullOrEmpty(step.Power))
                {
                    throw new RoutineScheduleConflictException($"PWM output '{outputVariableName}' cannot use 'power'. Use 'dutyCycle' instead.");
                }
                if (!step.DutyCycle.HasValue)
                {
                    throw new RoutineScheduleConflictException($"PWM output '{outputVariableName}' requires 'dutyCycle' parameter ({ActuatorConstants.Validation.MinDutyCycle}-{ActuatorConstants.Validation.MaxDutyCycle}).");
                }
                break;
        }
    }
}