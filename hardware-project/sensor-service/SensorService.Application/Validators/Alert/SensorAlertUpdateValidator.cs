using FluentValidation;
using SensorService.Application.DTOs.Alert;

namespace SensorService.Application.Validators.Alert;

public class SensorAlertUpdateValidator : AbstractValidator<SensorAlertUpdateDto>
{
    public SensorAlertUpdateValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("El campo Id no puede ser nullo");
    }
}
