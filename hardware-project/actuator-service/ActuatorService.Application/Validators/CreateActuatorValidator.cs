using FluentValidation;
using HydroEspinaca.Shared.DTOs.Actuator;
using HydroEspinaca.Shared.Validations;

namespace ActuatorService.Application.Validators.Actuators;

public class CreateActuatorValidator : AbstractValidator<CreateActuatorDto>
{
    public CreateActuatorValidator()
    {
        RuleFor(x => x.Esp32Id)
            .NotEmpty()
            .WithMessage("Debe especificarse un ESP32.")
            .BeValidObjectId();

        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("El nombre del actuador es obligatorio.")
            .MaximumLength(50).WithMessage("El nombre no debe superar los 50 caracteres.");

        RuleFor(x => x.Type)
            .NotEmpty()
            .WithMessage("Debe especificarse un tipo de actuador.");

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
