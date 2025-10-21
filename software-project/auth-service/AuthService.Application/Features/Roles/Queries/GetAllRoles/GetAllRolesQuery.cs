using HydroEspinaca.Shared.DTOs.Authentication;
using MediatR;

namespace AuthService.Application.Features.Roles.Queries.GetAllRoles;

public record GetAllRolesQuery() : IRequest<List<RoleResponseDto>>;