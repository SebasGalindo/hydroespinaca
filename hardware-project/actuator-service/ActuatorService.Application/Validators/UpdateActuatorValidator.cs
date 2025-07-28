using HydroEspinaca.Shared.DTOs.Actuator;
using FluentValidation;

namespace ActuatorService.Application.Validators.Actuators;

public class UpdateActuatorValidator : AbstractValidator<UpdateActuatorDto>
{
    public UpdateActuatorValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("El nombre del actuador es obligatorio.");

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
