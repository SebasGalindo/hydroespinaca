// No Application layer dependencies
using AuthService.Application.Exceptions;
using AuthService.Domain.Enums;
using AuthService.Domain.Interfaces;
using MediatR;
using RefreshTokenEntity = AuthService.Domain.Entities.RefreshToken;

namespace AuthService.Application.Features.Authentication.Commands.RefreshToken;

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, TokenResult>
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly ITokenService _tokenService;

    public RefreshTokenCommandHandler(
        IRefreshTokenRepository refreshTokenRepository,
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        ITokenService tokenService)
    {
        _refreshTokenRepository = refreshTokenRepository;
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _tokenService = tokenService;
    }

    public async Task<TokenResult> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var refreshToken = await _refreshTokenRepository.FindAsync(request.RefreshToken);
        if (refreshToken == null || refreshToken.ExpiresAt <= DateTime.UtcNow)
        {
            throw new InvalidRefreshTokenException();
        }

        var user = await _userRepository.FindByIdAsync(refreshToken.UserId);
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
            request.ClientId,
            tokenType);

        // Create new refresh token since current entity doesn't have update methods
        var newRefreshToken = new RefreshTokenEntity(
            user.Id,
            tokens.RefreshToken,
            tokens.ExpiresAt.AddDays(7),
            request.ClientId ?? HydroEspinaca.Shared.Constants.ClientIdentifiers.WebApp);
        
        await _refreshTokenRepository.AddAsync(newRefreshToken);

        return tokens;
    }
}