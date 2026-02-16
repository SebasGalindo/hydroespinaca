using MediatR;

namespace AuthService.Application.Features.Roles.Commands.DeleteRole;

/// <summary>
/// Command to delete a role by its identifier.
/// </summary>
public record DeleteRoleCommand(string IdOrCode) : IRequest<bool>;