using AuthService.Application.Shared.Validators;
using FluentValidation;

namespace AuthService.Application.Features.Permissions.Commands.UpdatePermission;

/// <summary>
/// Validator for permission update input.
/// </summary>
public class UpdatePermissionCommandValidator : BaseValidator<UpdatePermissionCommand>
{
    public UpdatePermissionCommandValidator()
    {
        RuleFor(x => x.IdOrCode)
            .NotEmpty()
            .WithMessage("El ID o código del permiso es requerido");

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