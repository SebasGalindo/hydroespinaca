using HydroEspinaca.Shared.DTOs.Authentication;
using MediatR;

namespace AuthService.Application.Features.Permissions.Commands.UpdatePermission;

/// <summary>
/// Command to update an existing permission's details.
/// </summary>
public record UpdatePermissionCommand(
    string IdOrCode,
    string Name,
    string? Description = null
) : IRequest<PermissionResponseDto?>;