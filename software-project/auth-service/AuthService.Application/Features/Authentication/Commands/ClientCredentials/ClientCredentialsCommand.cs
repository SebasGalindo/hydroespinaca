using AuthService.Domain.Interfaces;
using MediatR;

namespace AuthService.Application.Features.Authentication.Commands.ClientCredentials;

public record ClientCredentialsCommand(
    string ClientId,
    string ClientSecret
) : IRequest<TokenResult>;