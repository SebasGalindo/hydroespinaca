using AuthService.Application.Shared.Validators;
using FluentValidation;

namespace AuthService.Application.Features.Users.Commands.CreateUser;

/// <summary>
/// Validator for user creation input (email format, password strength, etc.).
/// </summary>
public class CreateUserCommandValidator : BaseValidator<CreateUserCommand>
{
    public CreateUserCommandValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty()
            .WithMessage("El nombre de usuario es requerido")
            .MaximumLength(100)
            .WithMessage("El nombre de usuario no puede tener más de 100 caracteres");

        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("El email es requerido")
            .EmailAddress()
            .WithMessage("El email no tiene un formato válido")
            .MaximumLength(255)
            .WithMessage("El email no puede tener más de 255 caracteres");

        RuleFor(x => x.Password)
            .NotEmpty()
            .WithMessage("La contraseña es requerida")
            .MinimumLength(8)
            .WithMessage("La contraseña debe tener al menos 8 caracteres")
            .MaximumLength(MaxPasswordLength)
            .WithMessage($"La contraseña no puede tener más de {MaxPasswordLength} caracteres");

        RuleFor(x => x.RoleId)
            .Length(ObjectIdLength)
            .WithMessage($"El ID del rol debe tener {ObjectIdLength} caracteres")
            .When(x => !string.IsNullOrEmpty(x.RoleId));
    }
}