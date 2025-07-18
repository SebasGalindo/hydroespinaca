using FluentValidation;
using SensorService.Application.DTOs.Mqtt;

namespace SensorService.Application.Validators.Mqtt;
public class ReadingInputValidator : AbstractValidator<ReadingInput>
{
    public ReadingInputValidator()
    {
        RuleFor(x => x.PhysicalId).NotEmpty();
        RuleFor(x => x.VariableId).NotEmpty();
        RuleFor(x => x.Value).NotNull();
    }
}

public class ReadingBatchValidator : AbstractValidator<ReadingBatchDto>
{
    public ReadingBatchValidator()
    {
        RuleFor(x => x.Esp32Id).NotEmpty();
        RuleFor(x => x.Timestamp)
            .NotEmpty()
            .LessThanOrEqualTo(DateTime.UtcNow.AddMinutes(5)); // margin of tolerance
        RuleFor(x => x.Readings)
            .NotNull().WithMessage("La lista de lecturas no puede ser nula.")
            .Must(r => r.Any()).WithMessage("Debe haber al menos una lectura.");
        RuleForEach(x => x.Readings).SetValidator(new ReadingInputValidator());
    }
}
