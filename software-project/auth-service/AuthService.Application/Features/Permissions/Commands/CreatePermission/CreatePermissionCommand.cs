using HydroEspinaca.Shared.DTOs.Authentication;
using MediatR;

namespace AuthService.Application.Features.Permissions.Commands.CreatePermission;

/// <summary>
/// Command to create a new authorization permission.
/// </summary>
public record CreatePermissionCommand(
    string Code,
    string Name,
    string? Description = null
) : IRequest<PermissionResponseDto>;