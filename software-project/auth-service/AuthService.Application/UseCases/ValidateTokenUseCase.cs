using AuthService.Application.Interfaces;

namespace AuthService.Application.UseCases;
public class ValidateTokenUseCase
{
    private readonly ITokenService _tokenService;

    public ValidateTokenUseCase(ITokenService tokenService)
    {
        _tokenService = tokenService;
    }
    public Task<bool> ExecuteAsync(string token)
    {
        bool isValid = _tokenService.IsTokenValid(token);
        return Task.FromResult(isValid);
    }
}