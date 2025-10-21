using AuthService.Application.Shared.Validators;
using FluentValidation;

namespace AuthService.Application.Features.Roles.Commands.UpdateRole;

public class UpdateRoleCommandValidator : BaseValidator<UpdateRoleCommand>
{
    public UpdateRoleCommandValidator()
    {
        RuleFor(x => x.IdOrCode)
            .NotEmpty()
            .WithMessage("El ID o código del rol es requerido");

        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("El nombre del rol es requerido")
            .Length(2, 100)
            .WithMessage("El nombre del rol debe tener entre 2 y 100 caracteres");

        RuleFor(x => x.PermissionCodes)
            .NotNull()
            .WithMessage("La lista de códigos de permisos no puede ser nula");

        RuleForEach(x => x.PermissionCodes)
            .Must(permCode => permCode.Contains(':'))
            .WithMessage("Cada código de permiso debe tener el formato 'resource:action' (ej: 'user:read', 'actuator:control')")
            .When(x => x.PermissionCodes != null && x.PermissionCodes.Count > 0);
    }
}