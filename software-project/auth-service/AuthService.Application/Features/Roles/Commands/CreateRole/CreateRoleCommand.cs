using HydroEspinaca.Shared.DTOs.Authentication;
using MediatR;

namespace AuthService.Application.Features.Roles.Commands.CreateRole;

public record CreateRoleCommand(
    string Code,
    string Name,
    List<string> PermissionCodes
) : IRequest<RoleResponseDto>;