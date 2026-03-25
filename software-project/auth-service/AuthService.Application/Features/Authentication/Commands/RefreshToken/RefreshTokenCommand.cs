using HydroEspinaca.Shared.DTOs.Authentication;
using MediatR;

namespace AuthService.Application.Features.Authentication.Commands.RefreshToken;

/// <summary>
/// Command for refreshing an expired access token using a valid refresh token.
/// </summary>
public record RefreshTokenCommand(
    string RefreshToken,
    string? ClientId = null,
    string? SessionId = null,
    string? IpAddress = null,
    string? UserAgent = null
) : IRequest<TokenResultDto>;