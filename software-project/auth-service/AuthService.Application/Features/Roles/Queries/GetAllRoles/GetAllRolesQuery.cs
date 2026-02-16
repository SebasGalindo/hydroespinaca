using HydroEspinaca.Shared.DTOs.Authentication;
using MediatR;

namespace AuthService.Application.Features.Roles.Queries.GetAllRoles;

/// <summary>
/// Query to retrieve all roles in the system.
/// </summary>
public record GetAllRolesQuery() : IRequest<List<RoleResponseDto>>;