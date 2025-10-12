using FluentValidation;
using HydroEspinaca.Shared.DTOs.Variables;

namespace SensorService.Application.Validators.Variable;

public class VariableCreateValidator : AbstractValidator<VariableCreateDto>
{

    public VariableCreateValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("El código es obligatorio.")
            .MaximumLength(20)
            .Matches("^[A-Z_]+$").WithMessage("El código debe contener solo letras mayúsculas y guiones bajos.");

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
            .GreaterThanOrEqualTo(x => x.PhysicalMin)
            .WithMessage("El valor óptimo mínimo no puede ser menor que el valor físico mínimo.")
            .LessThanOrEqualTo(x => x.PhysicalMax)
            .WithMessage("El valor óptimo mínimo no puede ser mayor que el valor físico máximo.");

        RuleFor(x => x.OptimalMax)
            .GreaterThan(x => x.OptimalMin)
            .WithMessage("El valor óptimo máximo debe ser mayor que el valor óptimo mínimo.")
            .LessThanOrEqualTo(x => x.PhysicalMax)
            .WithMessage("El valor óptimo máximo no puede ser mayor que el valor físico máximo.")
            .When(x => x.OptimalMax.HasValue);

        // Custom validation: OptimalMax, if provided, must be greater than OptimalMin
        RuleFor(x => x)
            .Must(x => !x.OptimalMax.HasValue || x.OptimalMax.Value > x.OptimalMin)
            .WithMessage("Si se proporciona un valor óptimo máximo, debe ser mayor que el valor óptimo mínimo.");

        RuleFor(x => x.Type)
          .NotEmpty()
          .WithMessage("El tipo de variable es obligatorio.");

        RuleFor(x => x.RegulationType)
          .Must(BeValidRegulationType)
          .WithMessage("El tipo de regulación debe ser 'Manual' o 'Automatic'.")
          .When(x => !string.IsNullOrEmpty(x.RegulationType));
    }

    private static bool BeValidRegulationType(string? regulationType)
    {
        if (string.IsNullOrEmpty(regulationType))
            return true; // Allow null/empty as it's optional

        return regulationType.Equals("Manual", StringComparison.OrdinalIgnoreCase) ||
               regulationType.Equals("Automatic", StringComparison.OrdinalIgnoreCase);
    }
}
