using AuthService.Application.Shared.Validators;
using FluentValidation;

namespace AuthService.Application.Features.Permissions.Queries.GetPermission;

public class GetPermissionQueryValidator : BaseValidator<GetPermissionQuery>
{
    public GetPermissionQueryValidator()
    {
        RuleFor(x => x.IdOrCode)
            .NotEmpty()
            .WithMessage("El ID o código del permiso es requerido");
    }
}