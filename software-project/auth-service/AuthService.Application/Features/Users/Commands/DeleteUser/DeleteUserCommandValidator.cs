using AuthService.Application.Shared.Validators;
using FluentValidation;

namespace AuthService.Application.Features.Users.Commands.DeleteUser;

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