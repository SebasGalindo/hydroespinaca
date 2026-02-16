using HydroEspinaca.Shared.DTOs.Authentication;
using MediatR;

namespace AuthService.Application.Features.Roles.Commands.CreateRole;

/// <summary>
/// Command to create a new authorization role with assigned permissions.
/// </summary>
public record CreateRoleCommand(
    string Code,
    string Name,
    List<string> PermissionCodes
) : IRequest<RoleResponseDto>;