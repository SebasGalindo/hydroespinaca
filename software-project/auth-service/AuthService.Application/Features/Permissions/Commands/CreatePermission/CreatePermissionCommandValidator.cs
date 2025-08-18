using AuthService.Application.Shared.Validators;
using FluentValidation;

namespace AuthService.Application.Features.Permissions.Commands.CreatePermission;

public class CreatePermissionCommandValidator : BaseValidator<CreatePermissionCommand>
{
    public CreatePermissionCommandValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty()
            .WithMessage("El código del permiso es requerido")
            .Must(code => code.StartsWith("perm_"))
            .WithMessage("El código del permiso debe comenzar con 'perm_'")
            .Length(5, 100)
            .WithMessage("El código del permiso debe tener entre 5 y 100 caracteres");

        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("El nombre del permiso es requerido")
            .Length(2, 100)
            .WithMessage("El nombre del permiso debe tener entre 2 y 100 caracteres");

        RuleFor(x => x.Description)
            .MaximumLength(500)
            .WithMessage("La descripción no puede tener más de 500 caracteres")
            .When(x => !string.IsNullOrEmpty(x.Description));
    }
}