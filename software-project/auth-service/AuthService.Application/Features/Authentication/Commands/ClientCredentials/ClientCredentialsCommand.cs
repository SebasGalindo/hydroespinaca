using HydroEspinaca.Shared.DTOs.Authentication;
using MediatR;

namespace AuthService.Application.Features.Authentication.Commands.ClientCredentials;

public record ClientCredentialsCommand(
    string ClientId,
    string ClientSecret
) : IRequest<TokenResultDto>;