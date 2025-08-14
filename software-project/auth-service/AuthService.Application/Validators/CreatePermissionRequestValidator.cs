using AuthService.Application.DTOs;
using AuthService.Domain.Interfaces;
using FluentValidation;

namespace AuthService.Application.Validators;

public class CreatePermissionRequestValidator : AbstractValidator<CreatePermissionRequestDto>
{
    private readonly IPermissionRepository _permissionRepository;

    public CreatePermissionRequestValidator(IPermissionRepository permissionRepository)
    {
        _permissionRepository = permissionRepository;

        RuleFor(x => x.Code)
            .NotEmpty()
            .WithMessage("El código del permiso es requerido")
            .Must(code => code.StartsWith("perm_"))
            .WithMessage("El código del permiso debe comenzar con 'perm_'")
            .Length(5, 100)
            .WithMessage("El código del permiso debe tener entre 5 y 100 caracteres")
            .MustAsync(async (code, cancellation) => !await _permissionRepository.ExistsByCodeAsync(code))
            .WithMessage("Ya existe un permiso con este código");

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