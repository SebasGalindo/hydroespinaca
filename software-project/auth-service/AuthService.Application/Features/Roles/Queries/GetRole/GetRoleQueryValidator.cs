using AuthService.Application.Shared.Validators;
using FluentValidation;

namespace AuthService.Application.Features.Roles.Queries.GetRole;

public class GetRoleQueryValidator : BaseValidator<GetRoleQuery>
{
    public GetRoleQueryValidator()
    {
        RuleFor(x => x.IdOrCode)
            .NotEmpty()
            .WithMessage("El ID o código del rol es requerido");
    }
}