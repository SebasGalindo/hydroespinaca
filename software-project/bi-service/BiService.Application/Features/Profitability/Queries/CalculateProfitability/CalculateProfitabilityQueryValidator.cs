using FluentValidation;

namespace BiService.Application.Features.Profitability.Queries.CalculateProfitability;

/// <summary>
/// Validador para la query de cálculo de rentabilidad.
/// </summary>
public class CalculateProfitabilityQueryValidator : AbstractValidator<CalculateProfitabilityQuery>
{
    public CalculateProfitabilityQueryValidator()
    {
        RuleFor(x => x.ProductionRecordId)
            .NotEmpty().WithMessage("El ID del registro de producción es requerido");

        RuleFor(x => x.ActuatorDurations)
            .NotNull().WithMessage("La lista de duraciones de actuadores no puede ser nula");

        RuleFor(x => x.InitialInvestmentCost)
            .GreaterThanOrEqualTo(0)
            .When(x => x.InitialInvestmentCost.HasValue)
            .WithMessage("El costo de inversión inicial no puede ser negativo");
    }
}
