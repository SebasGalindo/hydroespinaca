using FluentValidation;

namespace BiService.Application.Features.OperationalCost.Queries.CalculateOperationalCost;

/// <summary>
/// Validador para la query de cálculo de costo operacional.
/// </summary>
public class CalculateOperationalCostQueryValidator : AbstractValidator<CalculateOperationalCostQuery>
{
    public CalculateOperationalCostQueryValidator()
    {
        RuleFor(x => x.From)
            .NotEqual(default(DateTime)).WithMessage("La fecha 'from' es requerida");

        RuleFor(x => x.To)
            .NotEqual(default(DateTime)).WithMessage("La fecha 'to' es requerida");

        RuleFor(x => x)
            .Must(x => x.From <= x.To)
            .WithMessage("'from' debe ser menor o igual a 'to'");

        RuleFor(x => x.ActuatorDurations)
            .NotEmpty().WithMessage("Se requiere al menos un actuador con datos de duración");
    }
}
