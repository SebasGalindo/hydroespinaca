using AuthService.Application.Features.Permissions.DTOs;
using MediatR;

namespace AuthService.Application.Features.Permissions.Commands.CreatePermission;

public record CreatePermissionCommand(
    string Code,
    string Name,
    string? Description = null
) : IRequest<PermissionResponseDto>;