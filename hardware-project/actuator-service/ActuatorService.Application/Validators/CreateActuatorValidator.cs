using FluentValidation;
using HydroEspinaca.Shared.Constants;
using HydroEspinaca.Shared.DTOs.Actuator;
using HydroEspinaca.Shared.Validations;

namespace ActuatorService.Application.Validators;

/// <summary>
/// FluentValidation validator for actuator creation input.
/// </summary>
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
            .MaximumLength(ActuatorConstants.Validation.MaxCodeLength).WithMessage($"El código no debe superar los {ActuatorConstants.Validation.MaxCodeLength} caracteres.");

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
            .MaximumLength(ActuatorConstants.Validation.MaxLocationLength)
            .WithMessage($"La ubicación no debe superar los {ActuatorConstants.Validation.MaxLocationLength} caracteres.");

        RuleFor(x => x.PowerConsumptionWatts)
            .GreaterThanOrEqualTo(0)
            .WithMessage("El consumo de potencia en watts debe ser mayor o igual a 0.");
    }
}
