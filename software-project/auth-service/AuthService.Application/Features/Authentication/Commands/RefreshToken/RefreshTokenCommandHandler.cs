using AuthService.Application.Exceptions;
using AuthService.Domain.Enums;
using AuthService.Domain.Interfaces;
using HydroEspinaca.Shared.DTOs.Authentication;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AuthService.Application.Features.Authentication.Commands.RefreshToken;

/// <summary>
/// Handler that validates the refresh token, rotates it, and issues a new access token.
/// </summary>
public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, TokenResultDto>
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly ITokenService _tokenService;
    private readonly IUserSessionService _sessionService;
    private readonly IUserSessionRepository _sessionRepository;
    private readonly ILogger<RefreshTokenCommandHandler> _logger;

    public RefreshTokenCommandHandler(
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        ITokenService tokenService,
        IUserSessionService sessionService,
        IUserSessionRepository sessionRepository,
        ILogger<RefreshTokenCommandHandler> logger)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _tokenService = tokenService;
        _sessionService = sessionService;
        _sessionRepository = sessionRepository;
        _logger = logger;
    }

    public async Task<TokenResultDto> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var session = await _sessionRepository.FindByRefreshTokenAsync(request.RefreshToken);
        if (session == null)
        {
            _logger.LogWarning("Session not found by refresh token");
            throw new InvalidRefreshTokenException();
        }

        if (!session.IsActive)
        {
            _logger.LogWarning("Session is inactive: {SessionId}", session.SessionId);
            throw new InvalidRefreshTokenException();
        }

        var user = await _userRepository.FindByIdAsync(session.UserId);
        if (user == null)
        {
            _logger.LogWarning("User not found: {UserId}", session.UserId);
            throw new InvalidRefreshTokenException();
        }

        // Resolve role code
        string roleCode = "user";
        if (!string.IsNullOrEmpty(user.RoleId))
        {
            var role = await _roleRepository.FindByIdAsync(user.RoleId);
            if (role != null)
            {
                roleCode = role.Code;
            }
        }

        // Generate new tokens - PRESERVE original token type from the session
        var isUserSession = session.ClientId == "web" || session.ClientId == "mobile";
        var tokenType = isUserSession ? TokenType.User : TokenType.MachineToMachine;
        var clientIdForToken = session.ClientId;

        var tokens = await _tokenService.GenerateTokensAsync(
            user.Id,
            user.Email.Value,
            roleCode,
            clientIdForToken,
            tokenType,
            user.HasAcceptedTerms);

        // Use the ORIGINAL refresh token expiration from the session (set during login)
        var refreshTokenExpiresAt = session.ExpiresAt;

        await _sessionService.RefreshSessionAsync(
            request.RefreshToken,
            tokens.RefreshToken,
            tokens.AccessToken
        );

        _logger.LogInformation("Token refresh successful for session: {SessionId}", session.SessionId);

        return new TokenResultDto(
            tokens.AccessToken,
            tokens.RefreshToken,
            tokens.ExpiresAt,
            tokens.Role,
            user.Username,
            user.Email.Value,
            tokens.ClientId,
            tokens.Scopes,
            session.SessionId,
            refreshTokenExpiresAt
        );
    }
}