using HydroEspinaca.Shared.DTOs.Authentication;
using MediatR;

namespace AuthService.Application.Features.Authentication.Commands.RefreshToken;

public record RefreshTokenCommand(
    string RefreshToken,
    string? ClientId = null,
    string? SessionId = null,
    string? IpAddress = null,
    string? UserAgent = null
) : IRequest<TokenResultDto>;