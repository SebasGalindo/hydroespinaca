using FluentValidation;
using SensorService.Application.DTOs.Sensor;

namespace SensorService.Application.Validators.Sensor;

public class SensorCreateValidator : AbstractValidator<SensorCreateDto>
{
    public SensorCreateValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("El código es obligatorio.")
            .MaximumLength(50).WithMessage("El código no puede superar los 50 caracteres.");

        RuleFor(x => x.PhysicalId)
            .NotEmpty().WithMessage("El identificador físico es obligatorio.");

        RuleFor(x => x.Location)
            .NotEmpty().WithMessage("La ubicación es obligatoria.");

        RuleFor(x => x.Esp32Id)
            .NotEmpty().WithMessage("Debe especificarse un ESP32 válido.");

        RuleFor(x => x.SamplingFrequency)
            .GreaterThan(0).WithMessage("La frecuencia de muestreo debe ser mayor que cero.");

        RuleFor(x => x.Variables)
            .NotEmpty().WithMessage("Debe asociarse al menos una variable.");
    }
}
