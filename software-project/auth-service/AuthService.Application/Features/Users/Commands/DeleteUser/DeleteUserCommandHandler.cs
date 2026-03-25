using AuthService.Domain.Interfaces;
using MediatR;

namespace AuthService.Application.Features.Users.Commands.DeleteUser;

/// <summary>
/// Handler that removes a user and their associated sessions/tokens.
/// </summary>
public class DeleteUserCommandHandler : IRequestHandler<DeleteUserCommand>
{
    private readonly IUserRepository _userRepository;

    public DeleteUserCommandHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task Handle(DeleteUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.FindByIdAsync(request.Id);
        if (user == null)
        {
            throw new ArgumentException($"No se encontró el usuario con ID '{request.Id}'");
        }

        await _userRepository.DeleteAsync(request.Id);
    }
}