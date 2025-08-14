using AuthService.Application.DTOs;
using FluentValidation;

namespace AuthService.Application.Validators;

public class UpdatePermissionRequestValidator : AbstractValidator<UpdatePermissionRequestDto>
{
    public UpdatePermissionRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("El nombre del permiso es requerido")
            .Length(3, 200)
            .WithMessage("El nombre del permiso debe tener entre 3 y 200 caracteres");

        RuleFor(x => x.Description)
            .MaximumLength(500)
            .WithMessage("La descripción no puede exceder 500 caracteres");
    }
}