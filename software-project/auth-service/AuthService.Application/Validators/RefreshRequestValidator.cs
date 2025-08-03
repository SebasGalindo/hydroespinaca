using AuthService.Application.DTOs;
using FluentValidation;

namespace AuthService.Application.Validators;

public class RefreshRequestValidator : AbstractValidator<RefreshRequestDto>
{
    public RefreshRequestValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty().WithMessage("El token de refresco es obligatorio.");
    }
}
