using FluentValidation;
using HydroEspinaca.Shared.DTOs.Actuator;

namespace ActuatorService.Application.Validators;

public class MultiRoutineCommandValidator : AbstractValidator<MultiRoutineCommandDto>
{
    public MultiRoutineCommandValidator()
    {
        RuleFor(x => x.Routines)
            .NotEmpty()
            .WithMessage("At least one routine must be provided");

        RuleForEach(x => x.Routines)
            .SetValidator(new RoutineCommandValidator());
    }
}