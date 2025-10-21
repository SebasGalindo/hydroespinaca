using HydroEspinaca.Shared.DTOs.Authentication;
using MediatR;

namespace AuthService.Application.Features.Permissions.Queries.GetGroupedPermissions;

public record GetGroupedPermissionsQuery : IRequest<List<GroupedPermissionResponseDto>>;
