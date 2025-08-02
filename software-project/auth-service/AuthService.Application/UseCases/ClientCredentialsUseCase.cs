using AuthService.Application.DTOs;
using AuthService.Application.Interfaces;
using AuthService.Domain.Interfaces;

namespace AuthService.Application.UseCases
{
    public class ClientCredentialsUseCase
    {
        private readonly IClientAuthenticationService _clientAuthService;
        private readonly ITokenService _tokenService;

        public ClientCredentialsUseCase(
            IClientAuthenticationService clientAuthService,
            ITokenService tokenService)
        {
            _clientAuthService = clientAuthService;
            _tokenService = tokenService;
        }

        public async Task<TokenResponseDto> ExecuteAsync(string clientId, string clientSecret)
        {
            var app = await _clientAuthService.AuthenticateClientAsync(clientId, clientSecret);

            var tokens = _tokenService.GenerateTokens(
                userId: Guid.Empty,
                email: string.Empty,
                role: string.Join(',', app.Scopes),
                clientId: app.ClientId
            );

            return tokens with { RefreshToken = string.Empty };
        }
    }
}