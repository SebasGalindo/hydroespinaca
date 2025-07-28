using FluentValidation;
using HydroEspinaca.Shared.DTOs.Mqtt;
using HydroEspinaca.Shared.Validations;

namespace SensorService.Application.Validators.Mqtt;
public class ReadingInputValidator : AbstractValidator<ReadingInput>
{
    public ReadingInputValidator()
    {
        RuleFor(x => x.PhysicalId).NotEmpty().WithMessage("El identificador físico es obligatorio.");
        RuleFor(x => x.VariableId).NotEmpty().WithMessage("El ID de la variable es obligatorio.").BeValidObjectId();
        RuleFor(x => x.Value).NotNull().WithMessage("El valor de la lectura no puede ser nulo.");
    }
}

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
