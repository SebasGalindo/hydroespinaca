using AuthService.Domain.Interfaces;
using MediatR;

namespace AuthService.Application.Features.Authentication.Commands.RefreshToken;

public record RefreshTokenCommand(
    string RefreshToken,
    string? ClientId = null
) : IRequest<TokenResult>;