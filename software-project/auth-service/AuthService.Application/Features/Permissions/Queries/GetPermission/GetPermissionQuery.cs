using HydroEspinaca.Shared.DTOs.Authentication;
using MediatR;

namespace AuthService.Application.Features.Permissions.Queries.GetPermission;

/// <summary>
/// Query to retrieve a specific permission by its identifier.
/// </summary>
public record GetPermissionQuery(string IdOrCode) : IRequest<PermissionResponseDto?>;