using AuthService.Application.DTOs;
using AuthService.Domain.Interfaces;
using FluentValidation;

namespace AuthService.Application.Validators;

public class CreateRoleRequestValidator : AbstractValidator<CreateRoleRequestDto>
{
    private readonly IRoleRepository _roleRepository;
    private readonly IPermissionRepository _permissionRepository;

    public CreateRoleRequestValidator(IRoleRepository roleRepository, IPermissionRepository permissionRepository)
    {
        _roleRepository = roleRepository;
        _permissionRepository = permissionRepository;

        RuleFor(x => x.Code)
            .NotEmpty()
            .WithMessage("El código del rol es requerido")
            .Must(code => code.StartsWith("role_"))
            .WithMessage("El código del rol debe comenzar con 'role_'")
            .Length(5, 100)
            .WithMessage("El código del rol debe tener entre 5 y 100 caracteres")
            .MustAsync(async (code, cancellation) => !await _roleRepository.ExistsByCodeAsync(code))
            .WithMessage("Ya existe un rol con este código");

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

        RuleFor(x => x.PermissionCodes)
            .MustAsync(async (permissionCodes, cancellation) =>
            {
                if (permissionCodes == null || !permissionCodes.Any()) return true;
                var nonExistingCodes = await _permissionRepository.GetNonExistingCodesAsync(permissionCodes);
                return !nonExistingCodes.Any();
            })
            .WithMessage("Algunos códigos de permisos no existen")
            .When(x => x.PermissionCodes != null && x.PermissionCodes.Any());
    }
}