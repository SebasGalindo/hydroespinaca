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
        RuleFor(x => x.OutputVariable)
            .NotEmpty()
            .WithMessage("OutputVariable (Control Output ID) es requerido")
            .Must(ObjectIdHelper.IsValidObjectId)
            .WithMessage("OutputVariable debe ser un ObjectId válido");

        RuleFor(x => x)
            .Must(step => IsValidDuration(step))
            .WithMessage("Duration debe ser mayor a 0 segundos, excepto para comandos de control (power=Off o dutyCycle=0) que pueden tener duration=0");

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
                .InclusiveBetween(ActuatorConstants.Validation.MinDutyCycle, ActuatorConstants.Validation.MaxDutyCycle)
                .WithMessage($"DutyCycle debe estar entre {ActuatorConstants.Validation.MinDutyCycle} y {ActuatorConstants.Validation.MaxDutyCycle}");
        });
    }

    private bool HaveValidControlParameters(RoutineStepDto step)
    {
        bool hasPower = !string.IsNullOrEmpty(step.Power);
        bool hasDutyCycle = step.DutyCycle.HasValue;

        // Must have exactly one of them
        return hasPower ^ hasDutyCycle;
    }
    
    private bool IsValidDuration(RoutineStepDto step)
    {
        // Duration must be >= 0 for all cases
        if (step.Duration < 0)
            return false;
            
        // Duration = 0 is only allowed for control commands (power=Off or dutyCycle=0)
        if (step.Duration == 0)
        {
            bool isControlCommand = (step.Power == ActuatorConstants.PowerStates.Off) || 
                                  (step.DutyCycle == ActuatorConstants.Validation.MinDutyCycle);
            return isControlCommand;
        }
        
        // Duration > 0 is always valid
        return true;
    }
}