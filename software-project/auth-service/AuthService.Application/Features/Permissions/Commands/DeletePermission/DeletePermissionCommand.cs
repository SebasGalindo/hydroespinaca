using MediatR;

namespace AuthService.Application.Features.Permissions.Commands.DeletePermission;

public record DeletePermissionCommand(string IdOrCode) : IRequest<bool>;