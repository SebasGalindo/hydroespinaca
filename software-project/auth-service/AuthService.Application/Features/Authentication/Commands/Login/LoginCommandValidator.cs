using AuthService.Application.Shared.Validators;
using FluentValidation;

namespace AuthService.Application.Features.Authentication.Commands.Login;

/// <summary>
/// Validator ensuring email and password are provided for login.
/// </summary>
public class LoginCommandValidator : BaseValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
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
            .MinimumLength(MinPasswordLength)
            .WithMessage($"La contraseña debe tener al menos {MinPasswordLength} caracteres")
            .MaximumLength(MaxPasswordLength)
            .WithMessage($"La contraseña no puede tener más de {MaxPasswordLength} caracteres");
    }
}