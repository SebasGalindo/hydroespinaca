using FluentValidation;
using HydroEspinaca.Shared.DTOs.Actuator;

namespace ActuatorService.Application.Validators;

public class MultiRoutineCommandValidator : AbstractValidator<MultiRoutineCommandDto>
{
    public MultiRoutineCommandValidator()
    {
        RuleFor(x => x.Routines)
            .NotEmpty()
            .WithMessage("Al menos una rutina debe ser proporcionada");

        RuleForEach(x => x.Routines)
            .SetValidator(new RoutineCommandValidator());
    }
}