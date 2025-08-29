using FluentValidation;
using HydroEspinaca.Shared.Constants;
using HydroEspinaca.Shared.DTOs.Actuator;
using HydroEspinaca.Shared.Utils;

namespace ActuatorService.Application.Validators;

public class RoutineCommandValidator : AbstractValidator<RoutineCommandDto>
{
    public RoutineCommandValidator()
    {
        RuleFor(x => x.RoutineId)
            .NotEmpty()
            .WithMessage("RoutineId es requerido");

        RuleFor(x => x.Steps)
            .NotEmpty()
            .WithMessage("Se requiere al menos un paso");

        RuleForEach(x => x.Steps)
            .SetValidator(new RoutineStepValidator());
    }
}

public class RoutineStepValidator : AbstractValidator<RoutineStepDto>
{
    public RoutineStepValidator()
    {
        RuleFor(x => x.Actuator)
            .NotEmpty()
            .WithMessage("Actuator ID es requerido")
            .Must(ObjectIdHelper.IsValidObjectId)
            .WithMessage("Actuator ID debe ser un ObjectId válido");

        RuleFor(x => x.Duration)
            .GreaterThan(0)
            .WithMessage("Duration debe ser mayor a 0 segundos");

        // Ensure that either Power (for digital) or DutyCycle (for PWM) is provided, but not both
        RuleFor(x => x)
            .Must(HaveValidControlParameters)
            .WithMessage("El paso debe tener 'power' (para DIGITAL) o 'dutyCycle' (para PWM), pero no ambos");

        When(x => x.Power != null, () => {
            RuleFor(x => x.Power)
                .Must(power => ActuatorConstants.PowerStates.ValidPowerStates.Contains(power))
                .WithMessage($"Power debe ser '{ActuatorConstants.PowerStates.On}' o '{ActuatorConstants.PowerStates.Off}'");
        });

        When(x => x.DutyCycle.HasValue, () => {
            RuleFor(x => x.DutyCycle)
                .InclusiveBetween(0, 100)
                .WithMessage("DutyCycle debe estar entre 0 y 100");
        });
    }

    private bool HaveValidControlParameters(RoutineStepDto step)
    {
        bool hasPower = !string.IsNullOrEmpty(step.Power);
        bool hasDutyCycle = step.DutyCycle.HasValue;

        // Must have exactly one of them
        return hasPower ^ hasDutyCycle;
    }
}