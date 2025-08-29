using FluentValidation;
using HydroEspinaca.Shared.Constants;
using HydroEspinaca.Shared.DTOs.Actuator;

namespace ActuatorService.Application.Validators;

public class UpdateActuatorValidator : AbstractValidator<UpdateActuatorDto>
{
    public UpdateActuatorValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty()
            .WithMessage("El código/modelo del actuador es obligatorio.")
            .MaximumLength(100).WithMessage("El código no debe superar los 100 caracteres.");

        RuleFor(x => x.Mode)
            .NotEmpty()
            .WithMessage("El modo del actuador es obligatorio.")
            .Must(mode => ActuatorConstants.Modes.ValidModes.Contains(mode))
            .WithMessage($"El modo debe ser '{ActuatorConstants.Modes.Digital}' o '{ActuatorConstants.Modes.Pwm}'.");

        RuleFor(x => x.Pin)
            .NotEmpty()
            .WithMessage("El Pin es obligatorio.");

        RuleFor(x => x.Location)
            .NotEmpty()
            .WithMessage("La ubicación es obligatoria.");

        RuleFor(x => x.Status)
            .NotEmpty()
            .WithMessage("El estado del actuador es obligatorio.");
    }
}
