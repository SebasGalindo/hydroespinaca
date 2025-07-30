using HydroEspinaca.Shared.DTOs.Actuator;
using FluentValidation;

public class CommandMetadataDtoValidator : AbstractValidator<CommandMetadataDto>
{
    public CommandMetadataDtoValidator()
    {
        RuleFor(x => x.Source)
            .NotEmpty()
            .WithMessage("El campo 'Source' es obligatorio.");

        RuleFor(x => x.FuzzyRule)
            .MaximumLength(100)
            .WithMessage("La regla difusa no puede exceder los 100 caracteres.");

        RuleFor(x => x.Inputs)
            .Must(BeValidInputs)
            .When(x => x.Inputs is not null)
            .WithMessage("Las entradas deben tener valores numéricos válidos.");
    }

    private bool BeValidInputs(Dictionary<string, double>? inputs)
    {
        if (inputs is null)
            return true;

        return inputs.All(kvp => kvp.Value >= 0);
    }
}
