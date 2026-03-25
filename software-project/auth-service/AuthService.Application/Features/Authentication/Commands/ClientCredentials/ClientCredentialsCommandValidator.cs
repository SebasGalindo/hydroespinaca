using AuthService.Application.Shared.Validators;
using FluentValidation;

namespace AuthService.Application.Features.Authentication.Commands.ClientCredentials;

/// <summary>
/// Validator ensuring client_id and client_secret are provided.
/// </summary>
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