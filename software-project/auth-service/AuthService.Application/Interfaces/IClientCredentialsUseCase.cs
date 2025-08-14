using AuthService.Application.DTOs;

namespace AuthService.Application.Interfaces;

public interface IClientCredentialsUseCase
{
    Task<TokenResponseDto> ExecuteAsync(string clientId, string clientSecret);
}