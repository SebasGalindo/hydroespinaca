using AuthService.Application.Shared.Validators;
using FluentValidation;

namespace AuthService.Application.Features.Users.Queries.GetUser;

/// <summary>
/// Validator ensuring a valid user ID is provided.
/// </summary>
public class GetUserQueryValidator : BaseValidator<GetUserQuery>
{
    public GetUserQueryValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("El ID del usuario es requerido")
            .Length(ObjectIdLength)
            .WithMessage($"El ID del usuario debe tener {ObjectIdLength} caracteres");
    }
}