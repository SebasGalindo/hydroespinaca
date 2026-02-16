using HydroEspinaca.Shared.DTOs.Authentication;
using MediatR;

namespace AuthService.Application.Features.Permissions.Queries.GetAllPermissions;

/// <summary>
/// Query to retrieve all permissions in the system.
/// </summary>
public record GetAllPermissionsQuery() : IRequest<List<PermissionResponseDto>>;