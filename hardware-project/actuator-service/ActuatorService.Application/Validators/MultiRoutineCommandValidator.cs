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

        RuleFor(x => x.Routines)
            .Must(routines => routines.Select(r => r.RoutineId).Distinct().Count() == routines.Count)
            .WithMessage("No se permiten RoutineId duplicados en la misma petición");

        RuleForEach(x => x.Routines)
            .SetValidator(new RoutineCommandValidator());
    }
}