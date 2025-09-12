using AuthService.Application.Exceptions;
using AuthService.Domain.Enums;
using AuthService.Domain.Interfaces;
using HydroEspinaca.Shared.DTOs.Authentication;
using MediatR;
using RefreshTokenEntity = AuthService.Domain.Entities.RefreshToken;

namespace AuthService.Application.Features.Authentication.Commands.Login;

public class LoginCommandHandler : IRequestHandler<LoginCommand, TokenResultDto>
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly IRefreshTokenRepository _refreshTokenRepository;

    public LoginCommandHandler(
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IPasswordHasher passwordHasher,
        ITokenService tokenService,
        IRefreshTokenRepository refreshTokenRepository)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _refreshTokenRepository = refreshTokenRepository;
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

        var refreshToken = new RefreshTokenEntity(
            user.Id,
            tokens.RefreshToken,
            tokens.ExpiresAt.AddDays(7),
            HydroEspinaca.Shared.Constants.ClientIdentifiers.WebApp);

        await _refreshTokenRepository.AddAsync(refreshToken);

        return new TokenResultDto(
            tokens.AccessToken,
            tokens.RefreshToken,
            tokens.ExpiresAt,
            tokens.Role,
            tokens.ClientId,
            tokens.Scopes
        );
    }
}