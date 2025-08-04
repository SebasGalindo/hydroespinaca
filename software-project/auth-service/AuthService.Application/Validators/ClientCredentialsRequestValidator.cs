using AuthService.Application.DTOs;
using FluentValidation;

namespace AuthService.Application.Validators;
public class ClientCredentialsRequestValidator : AbstractValidator<ClientCredentialsRequestDto>
{
    public ClientCredentialsRequestValidator()
    {
        RuleFor(x => x.ClientId).NotEmpty();
        RuleFor(x => x.ClientSecret).NotEmpty();
    }
}