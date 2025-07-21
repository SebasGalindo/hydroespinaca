using FluentValidation;
using HydroEspinaca.Shared.DTOs.Esp32;

namespace SensorService.Application.Validators.Esp32Node;

public class Esp32NodeCreateValidator : AbstractValidator<Esp32NodeCreateDto>
{
    public Esp32NodeCreateValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("El identificador del ESP32 es obligatorio.")
            .MaximumLength(50).WithMessage("El identificador no debe superar los 50 caracteres.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("El nombre del ESP32 es obligatorio.")
            .MaximumLength(100).WithMessage("El nombre no debe superar los 100 caracteres.");

        RuleFor(x => x.Location)
            .NotEmpty().WithMessage("La ubicación es obligatoria.")
            .MaximumLength(100).WithMessage("La ubicación no debe superar los 100 caracteres.");
    }
}
