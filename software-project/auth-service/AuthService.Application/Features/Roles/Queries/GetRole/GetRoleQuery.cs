using HydroEspinaca.Shared.DTOs.Authentication;
using MediatR;

namespace AuthService.Application.Features.Roles.Queries.GetRole;

public record GetRoleQuery(string IdOrCode) : IRequest<RoleResponseDto?>;