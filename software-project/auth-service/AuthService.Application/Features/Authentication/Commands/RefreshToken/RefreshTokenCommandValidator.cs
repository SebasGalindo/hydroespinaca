using AuthService.Application.Shared.Validators;
using FluentValidation;

namespace AuthService.Application.Features.Authentication.Commands.RefreshToken;

public class RefreshTokenCommandValidator : BaseValidator<RefreshTokenCommand>
{
    public RefreshTokenCommandValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty()
            .WithMessage("El refresh token es requerido");
    }
}