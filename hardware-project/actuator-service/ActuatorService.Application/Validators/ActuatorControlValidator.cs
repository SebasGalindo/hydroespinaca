using FluentValidation;
using HydroEspinaca.Shared.Constants;
using HydroEspinaca.Shared.DTOs.Actuator;

namespace ActuatorService.Application.Validators;

public class ActuatorControlValidator : AbstractValidator<ActuatorControlDto>
{
    // Tolerance for floating-point comparisons
    private const double FloatTolerance = 1e-9;

    public ActuatorControlValidator()
    {
        RuleFor(x => x.ActuatorCode)
            .NotEmpty()
            .WithMessage("ActuatorCode es requerido");

        RuleFor(x => x)
            .Must(cmd => IsValidDuration(cmd))
            .WithMessage("Duration debe ser mayor a 0 segundos, excepto para comandos de control (power=Off o dutyCycle=0) que pueden tener duration=0");

        // Ensure that either Power (for digital) or DutyCycle (for PWM) is provided, but not both
        RuleFor(x => x)
            .Must(HaveValidControlParameters)
            .WithMessage("El comando debe tener 'power' (para DIGITAL) o 'dutyCycle' (para PWM), pero no ambos");

        When(x => x.Power != null, () => {
            RuleFor(x => x.Power)
                .Must(power => ActuatorConstants.PowerStates.ValidPowerStates.Contains(power))
                .WithMessage($"Power debe ser '{ActuatorConstants.PowerStates.On}' o '{ActuatorConstants.PowerStates.Off}'");
        });

        When(x => x.DutyCycle.HasValue, () => {
            RuleFor(x => x.DutyCycle)
                .InclusiveBetween(ActuatorConstants.Validation.MinDutyCycle, ActuatorConstants.Validation.MaxDutyCycle)
                .WithMessage($"DutyCycle debe estar entre {ActuatorConstants.Validation.MinDutyCycle} y {ActuatorConstants.Validation.MaxDutyCycle}");
        });
    }

    private bool HaveValidControlParameters(ActuatorControlDto cmd)
    {
        bool hasPower = !string.IsNullOrEmpty(cmd.Power);
        bool hasDutyCycle = cmd.DutyCycle.HasValue;

        // Must have exactly one of them
        return hasPower ^ hasDutyCycle;
    }

    private bool IsValidDuration(ActuatorControlDto cmd)
    {
        // Duration must be >= 0 for all cases
        if (cmd.Duration < 0)
            return false;

        // Duration = 0 is only allowed for control commands (power=Off or dutyCycle=0)
        // Use tolerance for floating-point comparison
        if (Math.Abs(cmd.Duration) < FloatTolerance)
        {
            bool isControlCommand = (cmd.Power == ActuatorConstants.PowerStates.Off) ||
                                  (cmd.DutyCycle.HasValue && Math.Abs(cmd.DutyCycle.Value - ActuatorConstants.Validation.MinDutyCycle) < FloatTolerance);
            return isControlCommand;
        }

        // Duration > 0 is always valid
        return true;
    }
}

public class ExecuteCommandsValidator : AbstractValidator<ExecuteCommandsDto>
{
    public ExecuteCommandsValidator()
    {
        RuleFor(x => x.Commands)
            .NotEmpty()
            .WithMessage("Se requiere al menos un comando");

        RuleForEach(x => x.Commands)
            .SetValidator(new ActuatorControlValidator());
    }
}
