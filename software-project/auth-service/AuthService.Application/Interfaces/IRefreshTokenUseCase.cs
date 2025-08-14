using AuthService.Application.DTOs;

namespace AuthService.Application.Interfaces;

public interface IRefreshTokenUseCase
{
    Task<TokenResponseDto> ExecuteAsync(RefreshRequestDto request);
}