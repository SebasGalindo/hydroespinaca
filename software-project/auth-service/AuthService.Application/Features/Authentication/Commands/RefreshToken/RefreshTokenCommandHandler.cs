using AuthService.Application.Exceptions;
using AuthService.Domain.Enums;
using AuthService.Domain.Interfaces;
using HydroEspinaca.Shared.DTOs.Authentication;
using MediatR;

namespace AuthService.Application.Features.Authentication.Commands.RefreshToken;

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, TokenResultDto>
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly ITokenService _tokenService;
    private readonly IUserSessionService _sessionService;
    private readonly IUserSessionRepository _sessionRepository;

    public RefreshTokenCommandHandler(
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        ITokenService tokenService,
        IUserSessionService sessionService,
        IUserSessionRepository sessionRepository)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _tokenService = tokenService;
        _sessionService = sessionService;
        _sessionRepository = sessionRepository;
    }

    public async Task<TokenResultDto> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        // Find session by refresh token
        var session = await _sessionRepository.FindByRefreshTokenAsync(request.RefreshToken);
        if (session == null || !session.IsActive)
        {
            throw new InvalidRefreshTokenException();
        }

        // Validate user still exists
        var user = await _userRepository.FindByIdAsync(session.UserId);
        if (user == null)
        {
            throw new InvalidRefreshTokenException();
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

        // Generate new tokens - determine token type based on client context
        var tokenType = string.IsNullOrEmpty(request.ClientId) ? TokenType.User : TokenType.MachineToMachine;
        var tokens = await _tokenService.GenerateTokensAsync(
            user.Id,
            user.Email.Value,
            roleCode,
            request.ClientId ?? session.ClientId,
            tokenType);

        // Calculate refresh token expiration
        var refreshTokenExpiresAt = tokens.ExpiresAt.AddDays(7);

        // Update session with new tokens
        await _sessionService.RefreshSessionAsync(
            request.RefreshToken,
            tokens.RefreshToken,
            tokens.AccessToken,
            refreshTokenExpiresAt
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
            session.SessionId,
            refreshTokenExpiresAt
        );
    }
}