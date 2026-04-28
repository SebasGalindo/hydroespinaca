using MediatR;

namespace AuthService.Application.Features.Users.Commands.AcceptTerms;

public record AcceptTermsCommand(string UserId) : IRequest;
