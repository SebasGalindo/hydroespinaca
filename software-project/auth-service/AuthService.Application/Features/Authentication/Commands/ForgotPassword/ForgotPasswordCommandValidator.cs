using FluentValidation;

namespace AuthService.Application.Features.Authentication.Commands.ForgotPassword;

/// <summary>
/// Validator for forgot password command
/// </summary>
public class ForgotPasswordCommandValidator : AbstractValidator<ForgotPasswordCommand>
{
    public ForgotPasswordCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("Email is required")
            .EmailAddress()
            .WithMessage("Invalid email format");
    }
}