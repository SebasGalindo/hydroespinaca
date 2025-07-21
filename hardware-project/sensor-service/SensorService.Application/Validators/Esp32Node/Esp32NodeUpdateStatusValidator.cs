using FluentValidation;
using HydroEspinaca.Shared.DTOs.Esp32;
using HydroEspinaca.Shared.Constants;

namespace SensorService.Application.Validators;

public class Esp32NodeUpdateStatusValidator : AbstractValidator<Esp32NodeUpdateStatusDto>
{
    public Esp32NodeUpdateStatusValidator()
    {
        RuleFor(x => x.Status)
            .NotEmpty().WithMessage("El estado es obligatorio.")
            .Must(s => Esp32Statuses.All.Contains(s))
            .WithMessage($"El estado debe ser uno de: {string.Join(", ", Esp32Statuses.All)}");
    }
}
