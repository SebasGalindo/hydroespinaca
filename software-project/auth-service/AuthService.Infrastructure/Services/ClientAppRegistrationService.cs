using AuthService.Application.Exceptions;
using AuthService.Domain.Entities;
using AuthService.Domain.Interfaces;
using AuthService.Domain.ValueObjects;

namespace AuthService.Infrastructure.Services;
/// <summary>
/// Infrastructure service for registering client applications and generating their credentials.
/// </summary>
public class ClientAppRegistrationService : IClientAppRegistrationService
{
    private readonly IClientAppRepository _repo;
    private readonly IPasswordHasher _hasher;

    public ClientAppRegistrationService(
        IClientAppRepository repo,
        IPasswordHasher hasher)
    {
        _repo = repo;
        _hasher = hasher;
    }

    public async Task RegisterAsync(string clientId, string secretPlain, IEnumerable<string> scopes)
    {
        if (await _repo.FindByClientIdAsync(clientId) != null)
            throw new ClientAppAlreadyExistsException(clientId);

        var hashed = _hasher.Hash(secretPlain);

        var app = new ClientApp(
            $"client_{clientId}",  // Code
            clientId,              // ClientId
            new HashedPassword(hashed),
            scopes
        );

        await _repo.AddAsync(app);
    }
}
