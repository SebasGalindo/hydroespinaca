using AuthService.Application.DTOs;
using AuthService.Application.Interfaces;
using AuthService.Domain.Interfaces;

namespace AuthService.Application.UseCases;

public class AuthenticateUserUseCase : IAuthenticateUserUseCase
{
    private readonly IAuthenticationService _authService;
    private readonly ITokenService _tokenService;

    public AuthenticateUserUseCase(
        IAuthenticationService authService,
        ITokenService tokenService)
    {
        _authService = authService;
        _tokenService = tokenService;
    }

    public async Task<TokenResponseDto> ExecuteAsync(LoginRequestDto request)
    {
        var user = await _authService.AuthenticateAsync(request.Email, request.Password);

        var tokens = _tokenService.GenerateTokens(
            Guid.Parse(user.Id),
            user.Email.Value,
            user.RoleId ?? "",
            clientId: null);

        return tokens;
    }
}
