using FluentValidation;
using SensorService.Application.DTOs.Alert;

namespace SensorService.Application.Validators.Alert;

public class SensorAlertUpdateValidator : AbstractValidator<SensorAlertUpdateDto>
{
    public SensorAlertUpdateValidator()
    {
        RuleFor(x => x.Acknowledged)
            .NotNull()
            .WithMessage("El campo Acknowledged no puede ser nulo.");
    }
}
