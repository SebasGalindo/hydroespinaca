using HydroEspinaca.Shared.DTOs.Authentication;
using MediatR;

namespace AuthService.Application.Features.Authentication.Commands.Login;

public record LoginCommand(
    string Email,
    string Password,
    string? SessionId = null,
    string? IpAddress = null,
    string? UserAgent = null,
    string? CsrfToken = null
) : IRequest<TokenResultDto>;