using FluentValidation;
using HydroEspinaca.Shared.Constants;
using HydroEspinaca.Shared.DTOs.Variables;
using HydroEspinaca.Shared.Enums;

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

        // Validaciones para rangos físicos
        RuleFor(x => x.PhysicalMin)
            .LessThan(x => x.PhysicalMax)
            .WithMessage("El valor físico mínimo debe ser menor que el valor físico máximo.");

        RuleFor(x => x.PhysicalMax)
            .GreaterThan(x => x.PhysicalMin)
            .WithMessage("El valor físico máximo debe ser mayor que el valor físico mínimo.");

        // Validaciones para rangos óptimos
        RuleFor(x => x.OptimalMin)
            .LessThan(x => x.OptimalMax)
            .WithMessage("El valor óptimo mínimo debe ser menor que el valor óptimo máximo.")
            .GreaterThanOrEqualTo(x => x.PhysicalMin)
            .WithMessage("El valor óptimo mínimo no puede ser menor que el valor físico mínimo.");

        RuleFor(x => x.OptimalMax)
            .GreaterThan(x => x.OptimalMin)
            .WithMessage("El valor óptimo máximo debe ser mayor que el valor óptimo mínimo.")
            .LessThanOrEqualTo(x => x.PhysicalMax)
            .WithMessage("El valor óptimo máximo no puede ser mayor que el valor físico máximo.");

        RuleFor(x => x.Type)
          .NotEmpty()
          .WithMessage("El tipo de variable es obligatorio.");
    }
}
