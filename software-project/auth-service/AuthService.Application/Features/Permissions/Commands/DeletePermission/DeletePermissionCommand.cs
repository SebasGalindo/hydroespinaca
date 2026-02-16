using MediatR;

namespace AuthService.Application.Features.Permissions.Commands.DeletePermission;

/// <summary>
/// Command to delete a permission by its identifier.
/// </summary>
public record DeletePermissionCommand(string IdOrCode) : IRequest<bool>;