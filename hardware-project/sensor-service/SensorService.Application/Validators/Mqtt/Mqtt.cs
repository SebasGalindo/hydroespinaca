using FluentValidation;
using HydroEspinaca.Shared.DTOs.Mqtt;
using HydroEspinaca.Shared.Validations;

namespace SensorService.Application.Validators.Mqtt;

/// <summary>
/// Validador de FluentValidation para una lectura individual dentro de un lote MQTT.
/// Valida que el identificador físico, código de variable y valor no estén vacíos.
/// </summary>
public class ReadingInputValidator : AbstractValidator<ReadingInput>
{
    public ReadingInputValidator()
    {
        RuleFor(x => x.PhysicalId).NotEmpty().WithMessage("El identificador físico es obligatorio.");
        RuleFor(x => x.VariableCode).NotEmpty().WithMessage("El código de la variable es obligatorio.");
        RuleFor(x => x.Value).NotNull().WithMessage("El valor de la lectura no puede ser nulo.");
    }
}

/// <summary>
/// Validador de FluentValidation para un lote completo de lecturas recibido vía MQTT.
/// Valida el ID del ESP32, la marca de tiempo (con tolerancia de 5 minutos),
/// la presencia de lecturas y delega la validación individual a ReadingInputValidator.
/// </summary>
public class ReadingBatchValidator : AbstractValidator<ReadingBatchDto>
{
    public ReadingBatchValidator()
    {
        RuleFor(x => x.Esp32Id).NotEmpty().BeValidObjectId();
        RuleFor(x => x.Timestamp)
            .NotEmpty()
            .LessThanOrEqualTo(DateTime.UtcNow.AddMinutes(5)); // margin of tolerance
        RuleFor(x => x.Readings)
            .NotNull().WithMessage("La lista de lecturas no puede ser nula.")
            .Must(r => r.Any()).WithMessage("Debe haber al menos una lectura.");
        RuleForEach(x => x.Readings).SetValidator(new ReadingInputValidator());
    }
}
