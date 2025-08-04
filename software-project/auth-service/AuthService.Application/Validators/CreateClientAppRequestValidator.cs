using AuthService.Application.DTOs;
using FluentValidation;

namespace AuthService.Application.Validators;
public class CreateClientAppRequestValidator : AbstractValidator<CreateClientAppRequestDto>
{
    public CreateClientAppRequestValidator()
    {
        RuleFor(x => x.ClientId)
            .NotEmpty().WithMessage("ClientId es obligatorio.")
            .MinimumLength(3).WithMessage("ClientId debe tener al menos 3 caracteres.");

        RuleFor(x => x.Secret)
            .NotEmpty().WithMessage("Secret es obligatorio.")
            .MinimumLength(8).WithMessage("Secret debe tener al menos 8 caracteres.");

        RuleFor(x => x.Scopes)
            .NotNull()
            .Must(scopes => scopes.Any())
            .WithMessage("Debe especificar al menos un scope.");
    }
}