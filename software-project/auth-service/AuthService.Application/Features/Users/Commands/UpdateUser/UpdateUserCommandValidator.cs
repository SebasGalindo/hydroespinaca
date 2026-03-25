using AuthService.Application.Shared.Validators;
using FluentValidation;

namespace AuthService.Application.Features.Users.Commands.UpdateUser;

/// <summary>
/// Validator for user update input.
/// </summary>
public class UpdateUserCommandValidator : BaseValidator<UpdateUserCommand>
{
    public UpdateUserCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("El ID del usuario es requerido")
            .Length(ObjectIdLength)
            .WithMessage($"El ID del usuario debe tener {ObjectIdLength} caracteres");

        RuleFor(x => x.Email)
            .EmailAddress()
            .WithMessage("El email no tiene un formato válido")
            .MaximumLength(255)
            .WithMessage("El email no puede tener más de 255 caracteres")
            .When(x => !string.IsNullOrEmpty(x.Email));

        RuleFor(x => x.Password)
            .MinimumLength(MinPasswordLength)
            .WithMessage($"La contraseña debe tener al menos {MinPasswordLength} caracteres")
            .MaximumLength(MaxPasswordLength)
            .WithMessage($"La contraseña no puede tener más de {MaxPasswordLength} caracteres")
            .When(x => !string.IsNullOrEmpty(x.Password));

        RuleFor(x => x.RoleId)
            .Length(ObjectIdLength)
            .WithMessage($"El ID del rol debe tener {ObjectIdLength} caracteres")
            .When(x => !string.IsNullOrEmpty(x.RoleId));
    }
}