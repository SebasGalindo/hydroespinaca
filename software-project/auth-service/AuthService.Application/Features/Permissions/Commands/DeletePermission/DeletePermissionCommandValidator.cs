using AuthService.Application.Shared.Validators;
using FluentValidation;

namespace AuthService.Application.Features.Permissions.Commands.DeletePermission;

/// <summary>
/// Validator ensuring a valid permission ID is provided for deletion.
/// </summary>
public class DeletePermissionCommandValidator : BaseValidator<DeletePermissionCommand>
{
    public DeletePermissionCommandValidator()
    {
        RuleFor(x => x.IdOrCode)
            .NotEmpty()
            .WithMessage("El ID o código del permiso es requerido");
    }
}