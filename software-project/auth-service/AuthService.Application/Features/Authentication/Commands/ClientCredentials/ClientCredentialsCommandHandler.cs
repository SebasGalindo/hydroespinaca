using AuthService.Application.Exceptions;
using AuthService.Domain.Enums;
using AuthService.Domain.Interfaces;
using MediatR;

namespace AuthService.Application.Features.Authentication.Commands.ClientCredentials;

public class ClientCredentialsCommandHandler : IRequestHandler<ClientCredentialsCommand, TokenResult>
{
    private readonly IClientAppRepository _clientAppRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly IPermissionRepository _permissionRepository;

    public ClientCredentialsCommandHandler(
        IClientAppRepository clientAppRepository,
        IPasswordHasher passwordHasher,
        ITokenService tokenService,
        IPermissionRepository permissionRepository)
    {
        _clientAppRepository = clientAppRepository;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _permissionRepository = permissionRepository;
    }

    public async Task<TokenResult> Handle(ClientCredentialsCommand request, CancellationToken cancellationToken)
    {
        var clientApp = await _clientAppRepository.FindByClientIdAsync(request.ClientId);
        if (clientApp == null)
        {
            throw new InvalidClientCredentialsException();
        }

        var isValidSecret = _passwordHasher.Verify(request.ClientSecret, clientApp.Secret.Value);
        if (!isValidSecret)
        {
            throw new InvalidClientCredentialsException();
        }

        // ✅ Get actual scopes from database - convert permission IDs to scopes
        var permissions = await _permissionRepository.FindByIdsAsync(clientApp.Scopes);
        var scopes = permissions.Select(p => p.Code).ToArray();

        // ✅ Use explicit scopes from database instead of hardcoded ones
        var tokens = await _tokenService.GenerateTokensAsync(
            clientApp.Id,
            clientApp.Code,
            HydroEspinaca.Shared.Constants.SystemRoles.Client,
            clientApp.Code,
            TokenType.MachineToMachine,
            scopes);  // ✅ Pass actual scopes from database

        return tokens;
    }
}