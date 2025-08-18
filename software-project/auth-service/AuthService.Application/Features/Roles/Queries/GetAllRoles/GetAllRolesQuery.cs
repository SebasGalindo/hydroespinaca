using AuthService.Application.Features.Roles.DTOs;
using MediatR;

namespace AuthService.Application.Features.Roles.Queries.GetAllRoles;

public record GetAllRolesQuery() : IRequest<List<RoleResponseDto>>;