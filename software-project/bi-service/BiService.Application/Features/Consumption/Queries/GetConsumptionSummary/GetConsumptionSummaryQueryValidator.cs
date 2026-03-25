using FluentValidation;

namespace BiService.Application.Features.Consumption.Queries.GetConsumptionSummary;

/// <summary>
/// Validador para la query de resumen de consumo.
/// </summary>
public class GetConsumptionSummaryQueryValidator : AbstractValidator<GetConsumptionSummaryQuery>
{
    public GetConsumptionSummaryQueryValidator()
    {
        RuleFor(x => x.From)
            .NotEqual(default(DateTime))
            .WithMessage("La fecha 'from' es requerida");

        RuleFor(x => x.To)
            .NotEqual(default(DateTime))
            .WithMessage("La fecha 'to' es requerida");

        RuleFor(x => x)
            .Must(x => x.From <= x.To)
            .WithMessage("'from' debe ser menor o igual a 'to'");
    }
}
