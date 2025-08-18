using AuthService.Application.Shared.Validators;
using FluentValidation;

namespace AuthService.Application.Features.Roles.Commands.CreateRole;

public class CreateRoleCommandValidator : BaseValidator<CreateRoleCommand>
{
    public CreateRoleCommandValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty()
            .WithMessage("El código del rol es requerido")
            .Must(code => code.StartsWith("role_"))
            .WithMessage("El código del rol debe comenzar con 'role_'")
            .Length(5, 100)
            .WithMessage("El código del rol debe tener entre 5 y 100 caracteres");

        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("El nombre del rol es requerido")
            .Length(2, 100)
            .WithMessage("El nombre del rol debe tener entre 2 y 100 caracteres");

        RuleFor(x => x.PermissionCodes)
            .NotNull()
            .WithMessage("La lista de códigos de permisos no puede ser nula");

        RuleForEach(x => x.PermissionCodes)
            .Must(permCode => permCode.StartsWith("perm_"))
            .WithMessage("Cada código de permiso debe comenzar con 'perm_'")
            .When(x => x.PermissionCodes != null);
    }
}