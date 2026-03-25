using AuthService.Application.Shared.Validators;
using FluentValidation;

namespace AuthService.Application.Features.Users.Commands.DeleteUser;

/// <summary>
/// Validator ensuring a valid user ID is provided for deletion.
/// </summary>
public class DeleteUserCommandValidator : BaseValidator<DeleteUserCommand>
{
    public DeleteUserCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("El ID del usuario es requerido")
            .Length(ObjectIdLength)
            .WithMessage($"El ID del usuario debe tener {ObjectIdLength} caracteres");
    }
}