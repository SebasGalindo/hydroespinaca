using AuthService.Application.Exceptions;
using AuthService.Domain.Enums;
using AuthService.Domain.Interfaces;
using HydroEspinaca.Shared.DTOs.Authentication;
using MediatR;

namespace AuthService.Application.Features.Authentication.Commands.Login;

public class LoginCommandHandler : IRequestHandler<LoginCommand, TokenResultDto>
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly IUserSessionService _sessionService;
    private readonly IUserSessionRepository _sessionRepository;

    public LoginCommandHandler(
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IPasswordHasher passwordHasher,
        ITokenService tokenService,
        IUserSessionService sessionService,
        IUserSessionRepository sessionRepository)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _sessionService = sessionService;
        _sessionRepository = sessionRepository;
    }

    public async Task<TokenResultDto> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.FindByEmailAsync(request.Email);
        if (user == null)
        {
            throw new InvalidCredentialsException();
        }

        var isValidPassword = _passwordHasher.Verify(user.Password.Value, request.Password);
        if (!isValidPassword)
        {
            throw new InvalidCredentialsException();
        }

        // Resolver el código del rol en lugar del RoleId
        string roleCode = "user"; // valor por defecto
        if (!string.IsNullOrEmpty(user.RoleId))
        {
            var role = await _roleRepository.FindByIdAsync(user.RoleId);
            if (role != null)
            {
                roleCode = role.Code;
            }
        }

        var tokens = await _tokenService.GenerateTokensAsync(
            user.Id,
            user.Email.Value,
            roleCode,
            null,
            TokenType.User);

        // Calculate refresh token expiration
        var refreshTokenExpiresAt = tokens.ExpiresAt.AddDays(7);

        // Limit to a maximum of 2 active sessions per user
        // If user has 2 or more active sessions, revoke the oldest ones
        const int MAX_SESSIONS = 2;
        var activeSessions = await _sessionService.GetActiveUserSessionsAsync(user.Id);
        var sessionsList = activeSessions.ToList();

        if (sessionsList.Count >= MAX_SESSIONS)
        {
            // Calculate how many sessions need to be revoked
            // We want to keep (MAX_SESSIONS - 1) sessions, so after creating the new one we'll have MAX_SESSIONS
            var sessionsToRevoke = sessionsList
                .OrderBy(s => s.LastActivity)  // Order by oldest activity first
                .Take(sessionsList.Count - MAX_SESSIONS + 1);  // Revoke oldest sessions

            foreach (var oldSession in sessionsToRevoke)
            {
                oldSession.Revoke();
                await _sessionRepository.UpdateAsync(oldSession);
            }
        }

        // Generate SessionId if not provided by BFF
        var sessionId = request.SessionId ?? Guid.NewGuid().ToString("N");

        // Create user session
        await _sessionService.CreateSessionAsync(
            user.Id,
            HydroEspinaca.Shared.Constants.ClientIdentifiers.WebApp,
            sessionId,
            tokens.RefreshToken,
            tokens.AccessToken,
            refreshTokenExpiresAt,
            request.IpAddress,
            request.UserAgent,
            request.CsrfToken
        );

        return new TokenResultDto(
            tokens.AccessToken,
            tokens.RefreshToken,
            tokens.ExpiresAt,
            tokens.Role,
            user.Username,
            user.Email.Value,
            tokens.ClientId,
            tokens.Scopes,
            sessionId,
            refreshTokenExpiresAt
        );
    }
}