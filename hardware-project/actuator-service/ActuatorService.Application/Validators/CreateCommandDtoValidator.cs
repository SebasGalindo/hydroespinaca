using FluentValidation;
using HydroEspinaca.Shared.DTOs.Actuator;
using HydroEspinaca.Shared.Enums;
using HydroEspinaca.Shared.Validations;

namespace ActuatorService.Application.Validators;

public class CreateCommandDtoValidator : AbstractValidator<CreateCommandDto>
{
    public CreateCommandDtoValidator()
    {
        RuleFor(x => x.ActuatorId)
            .NotEmpty()
            .WithMessage("El identificador del actuador es obligatorio")
            .BeValidObjectId();

        RuleFor(x => x.Esp32Id)
            .NotEmpty()
            .WithMessage("El indentificador del ESP32 es obligatorio")
            .BeValidObjectId();

        RuleFor(x => x.Action)
            .NotEmpty()
            .WithMessage("La acción es obligatoria");

        RuleFor(x => x.DurationMs)
            .GreaterThan(0)
            .WithMessage("La duración en milisegundos debe ser mayor que 0")
            .When(x => x.DurationMs.HasValue);

        RuleFor(x => x.Trigger)
         .NotEmpty()
         .WithMessage("El tipo de disparador es obligatorio");

        RuleFor(x => x.Metadata)
            .SetValidator(new CommandMetadataDtoValidator()!)
            .When(x => x.Metadata is not null);
    }
}
