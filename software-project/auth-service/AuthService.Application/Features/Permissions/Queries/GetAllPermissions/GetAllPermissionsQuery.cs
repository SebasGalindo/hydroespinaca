using AuthService.Application.Features.Permissions.DTOs;
using MediatR;

namespace AuthService.Application.Features.Permissions.Queries.GetAllPermissions;

public record GetAllPermissionsQuery() : IRequest<List<PermissionResponseDto>>;