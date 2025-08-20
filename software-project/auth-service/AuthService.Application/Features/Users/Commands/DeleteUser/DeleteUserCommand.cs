using MediatR;

namespace AuthService.Application.Features.Users.Commands.DeleteUser;

public record DeleteUserCommand(string Id) : IRequest;