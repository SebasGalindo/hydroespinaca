using ActuatorService.Application.DTOs;
using ActuatorService.Application.Interfaces;
using HydroEspinaca.Shared.DTOs.Actuator;
using HydroEspinaca.Shared.Enums;

namespace ActuatorService.Application.Services;

public class PhysicalStepTransformer : IPhysicalStepTransformer
{
    public MqttStepDto TransformToPhysicalStep(ResolvedRoutineStepDto resolvedStep)
    {
        var physicalStep = new MqttStepDto
        {
            Pin = resolvedStep.Pin,
            Mode = resolvedStep.Mode.ToString(),
            Duration = resolvedStep.Duration
        };

        // Set control parameters based on mode
        switch (resolvedStep.Mode)
        {
            case ActuatorMode.DIGITAL:
                physicalStep.Power = resolvedStep.Power;
                // Don't include DutyCycle for digital
                break;

            case ActuatorMode.PWM:
                physicalStep.DutyCycle = resolvedStep.DutyCycle;
                // Don't include Power for PWM
                break;
        }

        return physicalStep;
    }
}
