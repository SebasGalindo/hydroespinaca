using ActuatorService.Domain.Interfaces;
using HydroEspinaca.Shared.DTOs.Actuator;
using HydroEspinaca.Shared.Enums;
using HydroEspinaca.Shared.Errors;

namespace ActuatorService.Application.Services;

public interface IRoutineValidationService
{
    Task ValidateRoutineStepsAsync(List<RoutineStepDto> steps);
}

public class RoutineValidationService : IRoutineValidationService
{
    private readonly IActuatorRepository _actuatorRepository;

    public RoutineValidationService(IActuatorRepository actuatorRepository)
    {
        _actuatorRepository = actuatorRepository;
    }

    public async Task ValidateRoutineStepsAsync(List<RoutineStepDto> steps)
    {
        var actuatorIds = steps.Select(s => s.Actuator).Distinct().ToList();
        var actuators = await _actuatorRepository.GetByIdsAsync(actuatorIds);

        var missingActuators = actuatorIds.Except(actuators.Select(a => a.Id)).ToList();
        if (missingActuators.Any())
        {
            throw new NotFoundException($"Actuators not found: {string.Join(", ", missingActuators)}");
        }

        var actuatorModeMap = actuators.ToDictionary(a => a.Id, a => a.Mode);

        foreach (var step in steps)
        {
            var actuatorMode = actuatorModeMap[step.Actuator];
            ValidateStepForMode(step, actuatorMode);
        }
    }

    private void ValidateStepForMode(RoutineStepDto step, ActuatorMode mode)
    {
        switch (mode)
        {
            case ActuatorMode.DIGITAL:
                if (step.DutyCycle.HasValue)
                {
                    throw new ConflictException($"Digital actuator {step.Actuator} cannot use 'dutyCycle'. Use 'power' instead.");
                }
                if (string.IsNullOrEmpty(step.Power))
                {
                    throw new ConflictException($"Digital actuator {step.Actuator} requires 'power' parameter (ON/OFF).");
                }
                break;

            case ActuatorMode.PWM:
                if (!string.IsNullOrEmpty(step.Power))
                {
                    throw new ConflictException($"PWM actuator {step.Actuator} cannot use 'power'. Use 'dutyCycle' instead.");
                }
                if (!step.DutyCycle.HasValue)
                {
                    throw new ConflictException($"PWM actuator {step.Actuator} requires 'dutyCycle' parameter (0-100).");
                }
                break;
        }
    }
}