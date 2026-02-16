using FluentValidation;
using HydroEspinaca.Shared.DTOs.Esp32;

namespace SensorService.Application.Validators;

/// <summary>
/// Validador de FluentValidation para la actualización de estado de nodos ESP32.
/// Valida que el campo de estado no esté vacío.
/// </summary>
public class Esp32NodeUpdateStatusValidator : AbstractValidator<Esp32NodeUpdateStatusDto>
{
    public Esp32NodeUpdateStatusValidator()
    {
        RuleFor(x => x.Status)
            .NotEmpty()
            .WithMessage("El estado no puede estar vacío.");
    }
}