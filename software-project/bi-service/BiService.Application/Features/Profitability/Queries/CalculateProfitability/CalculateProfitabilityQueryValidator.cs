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
            .NotEmpty().WithMessage("Se requiere al menos un actuador con datos de duración");
    }
}
