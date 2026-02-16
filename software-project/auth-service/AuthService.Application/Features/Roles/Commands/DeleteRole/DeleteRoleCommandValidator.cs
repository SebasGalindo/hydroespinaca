using AuthService.Application.Shared.Validators;
using FluentValidation;

namespace AuthService.Application.Features.Roles.Commands.DeleteRole;

/// <summary>
/// Validator ensuring a valid role ID is provided for deletion.
/// </summary>
public class DeleteRoleCommandValidator : BaseValidator<DeleteRoleCommand>
{
    public DeleteRoleCommandValidator()
    {
        RuleFor(x => x.IdOrCode)
            .NotEmpty()
            .WithMessage("El ID o código del rol es requerido");
    }
}