using AuthService.Application.Shared.Validators;
using FluentValidation;

namespace AuthService.Application.Features.Users.Queries.GetUser;

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