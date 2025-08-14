using AuthService.Application.DTOs;
using AuthService.Application.Interfaces;
using AuthService.Domain.Interfaces;

namespace AuthService.Application.UseCases;

public class RefreshTokenUseCase : IRefreshTokenUseCase
{
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly ITokenService _tokenService;

    public RefreshTokenUseCase(
        IRefreshTokenService refreshTokenService,
        ITokenService tokenService)
    {
        _refreshTokenService = refreshTokenService;
        _tokenService = tokenService;
    }

    public async Task<TokenResponseDto> ExecuteAsync(RefreshRequestDto request)
    {
        var tokenInfo = await _refreshTokenService.ValidateAndRotateAsync(request.RefreshToken);

        var tokens = _tokenService.GenerateTokens(
            userId: tokenInfo.UserId,
            email: tokenInfo.Email,
            role: tokenInfo.Role,
            clientId: tokenInfo.ClientId
        );

        return tokens;
    }
}