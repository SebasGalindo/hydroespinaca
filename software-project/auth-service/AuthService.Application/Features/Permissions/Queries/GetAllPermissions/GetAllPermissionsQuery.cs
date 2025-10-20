using HydroEspinaca.Shared.DTOs.Authentication;
using MediatR;

namespace AuthService.Application.Features.Permissions.Queries.GetAllPermissions;

public record GetAllPermissionsQuery() : IRequest<List<PermissionResponseDto>>;