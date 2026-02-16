using FluentValidation;
using HydroEspinaca.Shared.DTOs.Sensors;
using HydroEspinaca.Shared.Validations;
using SensorService.Domain.Interfaces;

namespace SensorService.Application.Validators.Sensor;

/// <summary>
/// Validador de FluentValidation para la creación de sensores.
/// Valida código, identificador físico, ubicación, ESP32 asociado,
/// frecuencia de muestreo y variables asignadas.
/// </summary>
public class SensorCreateValidator : AbstractValidator<SensorCreateDto>
{
    public SensorCreateValidator(
        IVariableRepository variableRepo,
        IEsp32NodeRepository esp32Repo)
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("El código es obligatorio.")
            .MaximumLength(50).WithMessage("El código no puede superar los 50 caracteres.");

        RuleFor(x => x.PhysicalId)
            .NotEmpty().WithMessage("El identificador físico es obligatorio.");

        RuleFor(x => x.Location)
            .NotEmpty().WithMessage("La ubicación es obligatoria.");

        RuleFor(x => x.Esp32Id)
            .NotEmpty().WithMessage("Debe especificarse un ESP32 válido.").BeValidObjectId();

        RuleFor(x => x.SamplingFrequency)
            .GreaterThan(0).WithMessage("La frecuencia de muestreo debe ser mayor que cero.");

        RuleFor(x => x.Variables)
            .NotEmpty().WithMessage("Debe asociarse al menos una variable.");
    }
}
