using FluentValidation;

namespace BiService.Application.Features.Consumption.Commands.CreateConsumptionEntry;

/// <summary>
/// Validador de FluentValidation para el comando de creación de entrada de consumo.
/// </summary>
public class CreateConsumptionEntryCommandValidator : AbstractValidator<CreateConsumptionEntryCommand>
{
    public CreateConsumptionEntryCommandValidator()
    {
        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .WithMessage("La cantidad debe ser mayor que 0");

        RuleFor(x => x.Type)
            .IsInEnum()
            .WithMessage("El tipo de consumo no es válido");

        RuleFor(x => x.DateFrom)
            .NotEqual(default(DateTime))
            .WithMessage("La fecha de inicio es requerida")
            .LessThanOrEqualTo(DateTime.UtcNow.AddDays(1))
            .WithMessage("La fecha de inicio no puede ser futura");

        RuleFor(x => x.DateTo)
            .GreaterThanOrEqualTo(x => x.DateFrom)
            .When(x => x.DateTo.HasValue)
            .WithMessage("La fecha de fin no puede ser anterior a la fecha de inicio");

        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("El UserId es requerido");
    }
}
