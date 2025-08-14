using AuthService.Application.DTOs;
using FluentValidation;

namespace AuthService.Application.Validators;
public class ClientCredentialsRequestValidator : AbstractValidator<ClientCredentialsRequestDto>
{
    public ClientCredentialsRequestValidator()
    {
        RuleFor(x => x.ClientId)
            .NotEmpty()
            .WithMessage("El ID del cliente es requerido");
        
        RuleFor(x => x.ClientSecret)
            .NotEmpty()
            .WithMessage("El secreto del cliente es requerido");
    }
}