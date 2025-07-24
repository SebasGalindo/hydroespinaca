using HydroEspinaca.Shared.DTOs.Actuator;
using FluentValidation;
using HydroEspinaca.Shared.Enums;

namespace ActuatorService.Application.Validators;

public class CreateCommandDtoValidator : AbstractValidator<CreateCommandDto>
{
    public CreateCommandDtoValidator()
    {
        RuleFor(x => x.ActuatorId).NotEmpty();
        RuleFor(x => x.Esp32Id).NotEmpty();
        RuleFor(x => x.Action).NotEmpty();
        RuleFor(x => x.Trigger).IsInEnum();

        // If DurationMs is required only for manual/fuzzy, add conditional logic here
        RuleFor(x => x.DurationMs)
            .GreaterThan(0)
            .When(x => x.Trigger != TriggerType.Routine)
            .WithMessage("DurationMs es necesario para desencadenadores no rutinarios.");

        RuleFor(x => x.Metadata)
            .SetValidator(new CommandMetadataDtoValidator()!)
            .When(x => x.Metadata is not null);

    }
}
