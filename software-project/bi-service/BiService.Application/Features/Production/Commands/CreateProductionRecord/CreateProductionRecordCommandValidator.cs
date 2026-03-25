using FluentValidation;

namespace BiService.Application.Features.Production.Commands.CreateProductionRecord;

/// <summary>
/// Validador para el comando de creación de registro de producción.
/// </summary>
public class CreateProductionRecordCommandValidator : AbstractValidator<CreateProductionRecordCommand>
{
    public CreateProductionRecordCommandValidator()
    {
        RuleFor(x => x.CropName)
            .NotEmpty().WithMessage("El nombre del cultivo es requerido")
            .MaximumLength(100).WithMessage("El nombre del cultivo no puede exceder 100 caracteres");

        RuleFor(x => x.StartDate)
            .NotEqual(default(DateTime)).WithMessage("La fecha de inicio es requerida");

        RuleFor(x => x.HarvestDate)
            .NotEqual(default(DateTime)).WithMessage("La fecha de cosecha es requerida");

        RuleFor(x => x)
            .Must(x => x.StartDate < x.HarvestDate)
            .WithMessage("La fecha de inicio debe ser anterior a la fecha de cosecha");

        RuleFor(x => x.KilosProduced)
            .GreaterThan(0).WithMessage("Los kilos producidos deben ser mayor que 0");

        RuleFor(x => x.PricePerKilo)
            .GreaterThanOrEqualTo(0).WithMessage("El precio por kilo no puede ser negativo");

        RuleFor(x => x.Currency)
            .NotEmpty().WithMessage("La moneda es requerida")
            .MaximumLength(10).WithMessage("La moneda no puede exceder 10 caracteres");

        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("El UserId es requerido");
    }
}
