using AuthService.Domain.Interfaces;
using MediatR;

namespace AuthService.Application.Features.Authentication.Commands.Login;

public record LoginCommand(
    string Email,
    string Password
) : IRequest<TokenResult>;