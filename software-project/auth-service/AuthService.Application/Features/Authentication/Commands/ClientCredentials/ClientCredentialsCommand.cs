using HydroEspinaca.Shared.DTOs.Authentication;
using MediatR;

namespace AuthService.Application.Features.Authentication.Commands.ClientCredentials;

/// <summary>
/// Command for authenticating a client application using client credentials flow (client_id + client_secret).
/// </summary>
public record ClientCredentialsCommand(
    string ClientId,
    string ClientSecret
) : IRequest<TokenResultDto>;