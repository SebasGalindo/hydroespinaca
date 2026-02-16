using HydroEspinaca.Shared.DTOs.Authentication;
using MediatR;

namespace AuthService.Application.Features.Permissions.Queries.GetGroupedPermissions;

/// <summary>
/// Query to retrieve permissions grouped by their service or domain area.
/// </summary>
public record GetGroupedPermissionsQuery : IRequest<List<GroupedPermissionResponseDto>>;
