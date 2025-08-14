using AuthService.Application.DTOs;

namespace AuthService.Application.Interfaces;

public interface IAuthenticateUserUseCase
{
    Task<TokenResponseDto> ExecuteAsync(LoginRequestDto request);
}