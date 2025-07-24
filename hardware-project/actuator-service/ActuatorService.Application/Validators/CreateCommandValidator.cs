using HydroEspinaca.Shared.DTOs.Actuator;
using FluentValidation;
using HydroEspinaca.Shared.Enums;

namespace ActuatorService.Application.Validators.Commands;

public class CreateCommandValidator : AbstractValidator<CreateCommandDto>
{
    public CreateCommandValidator()
    {
        RuleFor(x => x.ActuatorId).NotEmpty();
        RuleFor(x => x.Esp32Id).NotEmpty();
        RuleFor(x => x.Action).NotEmpty();
        RuleFor(x => x.DurationMs)
            .GreaterThan(0)
            .When(x => x.DurationMs.HasValue);

        RuleFor(x => x.Trigger)
            .IsInEnum();

        When(x => x.Trigger == TriggerType.Routine, () =>
        {
            RuleFor(x => x.RoutineId).NotEmpty();
            RuleFor(x => x.RoutineStepOrder)
                .NotNull()
                .GreaterThanOrEqualTo(0);
        });

        When(x => x.Metadata is not null, () =>
        {
            RuleFor(x => x.Metadata!.Source).NotEmpty();
        });
    }
}
