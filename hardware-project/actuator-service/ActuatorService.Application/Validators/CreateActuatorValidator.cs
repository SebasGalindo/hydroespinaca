using FluentValidation;
using HydroEspinaca.Shared.Constants;
using HydroEspinaca.Shared.DTOs.Actuator;
using HydroEspinaca.Shared.Validations;

namespace ActuatorService.Application.Validators;

public class CreateActuatorValidator : AbstractValidator<CreateActuatorDto>
{
    public CreateActuatorValidator()
    {
        RuleFor(x => x.Esp32Id)
            .NotEmpty()
            .WithMessage("Debe especificarse un ESP32.")
            .BeValidObjectId();

        RuleFor(x => x.Code)
            .NotEmpty()
            .WithMessage("El código/modelo del actuador es obligatorio.")
            .MaximumLength(100).WithMessage("El código no debe superar los 100 caracteres.");

        RuleFor(x => x.Type)
            .NotEmpty()
            .WithMessage("Debe especificarse un tipo de actuador.");

        RuleFor(x => x.Mode)
            .NotEmpty()
            .WithMessage($"Debe especificarse un modo de actuador ({ActuatorConstants.Modes.Digital} o {ActuatorConstants.Modes.Pwm}).")
            .Must(mode => ActuatorConstants.Modes.ValidModes.Contains(mode))
            .WithMessage($"El modo debe ser '{ActuatorConstants.Modes.Digital}' o '{ActuatorConstants.Modes.Pwm}'.");

        RuleFor(x => x.PhysicalId)
            .NotEmpty()
            .WithMessage("Debe especificarse un identificador físico.");

        RuleFor(x => x.Pin)
            .NotEmpty()
            .WithMessage("El Pin es obligatorio.");

        RuleFor(x => x.Location)
            .NotEmpty()
            .WithMessage("La ubicación es obligatoria.")
            .MaximumLength(100)
            .WithMessage("La ubicación no debe superar los 100 caracteres.");
    }
}
