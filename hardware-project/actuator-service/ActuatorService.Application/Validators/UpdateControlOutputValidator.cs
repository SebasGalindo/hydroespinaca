using FluentValidation;
using HydroEspinaca.Shared.DTOs.Actuator;
using HydroEspinaca.Shared.Validations;

namespace ActuatorService.Application.Validators;

public class UpdateControlOutputValidator : AbstractValidator<UpdateControlOutputDto>
{
    public UpdateControlOutputValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("El nombre del control output es obligatorio.")
            .MaximumLength(100)
            .WithMessage("El nombre no debe superar los 100 caracteres.");

        RuleFor(x => x.Description)
            .MaximumLength(500)
            .WithMessage("La descripción no debe superar los 500 caracteres.")
            .When(x => !string.IsNullOrEmpty(x.Description));

        RuleFor(x => x.Unit)
            .MaximumLength(50)
            .WithMessage("La unidad no debe superar los 50 caracteres.")
            .When(x => !string.IsNullOrEmpty(x.Unit));

        RuleFor(x => x.ActuatorId)
            .NotEmpty()
            .WithMessage("Debe especificarse un actuador.")
            .BeValidObjectId();

        RuleFor(x => x.MinValue)
            .NotNull()
            .WithMessage("El valor mínimo es obligatorio.");

        RuleFor(x => x.MaxValue)
            .NotNull()
            .WithMessage("El valor máximo es obligatorio.")
            .GreaterThan(x => x.MinValue)
            .WithMessage("El valor máximo debe ser mayor que el valor mínimo.");
    }
}
