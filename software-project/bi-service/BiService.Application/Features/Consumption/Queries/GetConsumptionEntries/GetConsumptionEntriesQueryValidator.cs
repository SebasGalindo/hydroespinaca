using FluentValidation;

namespace BiService.Application.Features.Consumption.Queries.GetConsumptionEntries;

/// <summary>
/// Validador para la query de consulta de entradas de consumo.
/// </summary>
public class GetConsumptionEntriesQueryValidator : AbstractValidator<GetConsumptionEntriesQuery>
{
    public GetConsumptionEntriesQueryValidator()
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

        RuleFor(x => x.Type)
            .IsInEnum()
            .WithMessage("El tipo de consumo no es válido")
            .When(x => x.Type.HasValue);
    }
}
