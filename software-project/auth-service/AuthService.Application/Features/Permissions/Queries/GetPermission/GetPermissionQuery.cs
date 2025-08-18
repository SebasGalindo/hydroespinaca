using AuthService.Application.Features.Permissions.DTOs;
using MediatR;

namespace AuthService.Application.Features.Permissions.Queries.GetPermission;

public record GetPermissionQuery(string IdOrCode) : IRequest<PermissionResponseDto?>;