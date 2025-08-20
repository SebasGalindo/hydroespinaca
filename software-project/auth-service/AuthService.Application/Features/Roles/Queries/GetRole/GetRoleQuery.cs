using AuthService.Application.Features.Roles.DTOs;
using MediatR;

namespace AuthService.Application.Features.Roles.Queries.GetRole;

public record GetRoleQuery(string IdOrCode) : IRequest<RoleResponseDto?>;