using HydroEspinaca.Shared.DTOs.Authentication;
using MediatR;

namespace AuthService.Application.Features.Roles.Queries.GetRole;

/// <summary>
/// Query to retrieve a specific role by its identifier.
/// </summary>
public record GetRoleQuery(string IdOrCode) : IRequest<RoleResponseDto?>;