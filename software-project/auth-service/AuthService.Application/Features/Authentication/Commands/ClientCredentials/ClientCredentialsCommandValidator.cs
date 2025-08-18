using AuthService.Application.Shared.Validators;
using FluentValidation;

namespace AuthService.Application.Features.Authentication.Commands.ClientCredentials;

public class ClientCredentialsCommandValidator : BaseValidator<ClientCredentialsCommand>
{
    public ClientCredentialsCommandValidator()
    {
        RuleFor(x => x.ClientId)
            .NotEmpty()
            .WithMessage("El Client ID es requerido");

        RuleFor(x => x.ClientSecret)
            .NotEmpty()
            .WithMessage("El Client Secret es requerido");
    }
}