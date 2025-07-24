using HydroEspinaca.Shared.DTOs.Actuator;
using FluentValidation;

namespace ActuatorService.Application.Validators.Actuators;

public class UpdateActuatorValidator : AbstractValidator<UpdateActuatorDto>
{
    public UpdateActuatorValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.Pin).NotEmpty();
        RuleFor(x => x.Location).NotEmpty();
        RuleFor(x => x.Status)
            .IsInEnum();
    }
}
