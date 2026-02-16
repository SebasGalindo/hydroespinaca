using MediatR;

namespace AuthService.Application.Features.Users.Commands.DeleteUser;

/// <summary>
/// Command to delete a user by their identifier.
/// </summary>
public record DeleteUserCommand(string Id) : IRequest;