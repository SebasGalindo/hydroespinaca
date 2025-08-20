using AuthService.Application.Features.Permissions.DTOs;
using MediatR;

namespace AuthService.Application.Features.Permissions.Commands.UpdatePermission;

public record UpdatePermissionCommand(
    string IdOrCode,
    string Name,
    string? Description = null
) : IRequest<PermissionResponseDto?>;