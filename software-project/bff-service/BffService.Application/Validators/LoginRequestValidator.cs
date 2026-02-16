using FluentValidation;
using HydroEspinaca.Shared.DTOs.Authentication;

namespace BffService.Application.Validators;

/// <summary>
/// FluentValidation validator for logout request input.
/// </summary>
public class LogoutRequestValidator : AbstractValidator<LogoutRequestDto>
{
    public LogoutRequestValidator()
    {
        RuleFor(x => x.SessionId)
            .NotEmpty()
            .WithMessage("Session ID es requerido");
    }
}

/// <summary>
/// FluentValidation validator for refresh token request input.
/// </summary>
public class RefreshTokenRequestValidator : AbstractValidator<RefreshTokenRequestDto>
{
    public RefreshTokenRequestValidator()
    {
        RuleFor(x => x.SessionId)
            .NotEmpty()
            .WithMessage("Session ID es requerido");
    }
}