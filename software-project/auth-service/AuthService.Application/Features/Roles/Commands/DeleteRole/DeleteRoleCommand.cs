using MediatR;

namespace AuthService.Application.Features.Roles.Commands.DeleteRole;

public record DeleteRoleCommand(string IdOrCode) : IRequest<bool>;