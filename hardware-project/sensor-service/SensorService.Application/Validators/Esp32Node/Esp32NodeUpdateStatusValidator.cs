using FluentValidation;
using HydroEspinaca.Shared.Constants;
using HydroEspinaca.Shared.DTOs.Esp32;
using HydroEspinaca.Shared.Enums;

namespace SensorService.Application.Validators;

public class Esp32NodeUpdateStatusValidator : AbstractValidator<Esp32NodeUpdateStatusDto>
{
    public Esp32NodeUpdateStatusValidator()
    {
        RuleFor(x => x.Status).IsInEnum()
            .WithMessage($"El estado debe ser uno de los siguientes: {string.Join(", ", Enum.GetNames(typeof(Esp32Status)))}"); 
    }
}
