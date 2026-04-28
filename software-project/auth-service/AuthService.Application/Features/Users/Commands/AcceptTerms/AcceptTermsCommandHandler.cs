using AuthService.Domain.Exceptions;
using AuthService.Domain.Interfaces;
using MediatR;

namespace AuthService.Application.Features.Users.Commands.AcceptTerms;

public class AcceptTermsCommandHandler : IRequestHandler<AcceptTermsCommand>
{
    private readonly IUserRepository _userRepository;

    public AcceptTermsCommandHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task Handle(AcceptTermsCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.FindByIdAsync(request.UserId);
        if (user == null)
            throw new UserNotFoundException(request.UserId);

        if (!user.HasAcceptedTerms)
        {
            user.AcceptTerms();
            await _userRepository.UpdateAsync(user);
        }
    }
}
