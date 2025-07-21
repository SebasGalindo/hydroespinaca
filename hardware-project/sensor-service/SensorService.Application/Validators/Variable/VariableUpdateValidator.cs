using FluentValidation;
using HydroEspinaca.Shared.Constants;
using HydroEspinaca.Shared.DTOs.Variables;

namespace SensorService.Application.Validators.Variable;

public class VariableUpdateValidator : AbstractValidator<VariableUpdateDto>
{
    public VariableUpdateValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("El nombre es obligatorio.")
            .MaximumLength(100);

        RuleFor(x => x.Unit)
            .NotEmpty().WithMessage("La unidad de medida es obligatoria.")
            .MaximumLength(20);

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("La descripción es obligatoria.")
            .MaximumLength(200);

        RuleFor(x => x.MinValue)
            .LessThan(x => x.MaxValue)
            .WithMessage("El valor mínimo debe ser menor que el valor máximo.");

        RuleFor(x => x.MaxValue)
            .GreaterThan(x => x.MinValue)
            .WithMessage("El valor máximo debe ser mayor que el valor mínimo.");

        RuleFor(x => x.Type)
            .Must(type => VariableTypes.All.Contains(type))
            .WithMessage($"El tipo debe ser uno de los siguientes: {string.Join(", ", VariableTypes.All)}.");

    }
}
