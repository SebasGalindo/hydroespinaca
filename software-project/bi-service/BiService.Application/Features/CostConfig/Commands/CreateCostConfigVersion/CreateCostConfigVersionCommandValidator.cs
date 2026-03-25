using FluentValidation;

namespace BiService.Application.Features.CostConfig.Commands.CreateCostConfigVersion;

/// <summary>
/// Validador de FluentValidation para el comando de creación de versión de configuración de costos.
/// </summary>
public class CreateCostConfigVersionCommandValidator : AbstractValidator<CreateCostConfigVersionCommand>
{
    public CreateCostConfigVersionCommandValidator()
    {
        RuleFor(x => x.Currency)
            .NotEmpty()
            .WithMessage("La moneda es requerida")
            .MaximumLength(10)
            .WithMessage("La moneda no puede tener más de 10 caracteres");

        RuleFor(x => x.ElectricityCostPerKwh)
            .GreaterThanOrEqualTo(0)
            .WithMessage("El costo de electricidad no puede ser negativo");

        RuleFor(x => x.WaterCostPerLiter)
            .GreaterThanOrEqualTo(0)
            .WithMessage("El costo de agua no puede ser negativo");

        RuleFor(x => x.NutrientCostPerLiter)
            .GreaterThanOrEqualTo(0)
            .WithMessage("El costo de nutrientes no puede ser negativo");

        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("El UserId es requerido");
    }
}
