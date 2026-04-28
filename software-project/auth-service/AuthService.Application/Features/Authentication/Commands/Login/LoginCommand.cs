using HydroEspinaca.Shared.DTOs.Authentication;
using MediatR;

namespace AuthService.Application.Features.Authentication.Commands.Login;

/// <summary>
/// Command for authenticating a user with email and password credentials.
/// </summary>
public record LoginCommand(
    string Email,
    string Password,
    string? ClientId = null,
    string? SessionId = null,
    string? IpAddress = null,
    string? UserAgent = null,
    string? CsrfToken = null,
    bool AcceptTerms = false
) : IRequest<TokenResultDto>;