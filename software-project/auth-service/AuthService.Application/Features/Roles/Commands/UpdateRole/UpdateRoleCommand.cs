using HydroEspinaca.Shared.DTOs.Authentication;
using MediatR;

namespace AuthService.Application.Features.Roles.Commands.UpdateRole;

public record UpdateRoleCommand(
    string IdOrCode,
    string Name,
    List<string> PermissionCodes
) : IRequest<RoleResponseDto?>;