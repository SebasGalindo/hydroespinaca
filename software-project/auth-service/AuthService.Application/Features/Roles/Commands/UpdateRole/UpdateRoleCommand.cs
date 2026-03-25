using HydroEspinaca.Shared.DTOs.Authentication;
using MediatR;

namespace AuthService.Application.Features.Roles.Commands.UpdateRole;

/// <summary>
/// Command to update an existing role's name, description, or permissions.
/// </summary>
public record UpdateRoleCommand(
    string IdOrCode,
    string Name,
    List<string> PermissionCodes
) : IRequest<RoleResponseDto?>;