using FluentValidation;
using HydroEspinaca.Shared.DTOs.Esp32;

namespace SensorService.Application.Validators;

public class Esp32NodeUpdateStatusValidator : AbstractValidator<Esp32NodeUpdateStatusDto>
{
    public Esp32NodeUpdateStatusValidator()
    {
        RuleFor(x => x.Status)
            .NotEmpty()
            .WithMessage("El estado no puede estar vacío.");
    }
}