using AuthService.Domain.Entities;

namespace AuthService.Domain.Interfaces;
/// <summary>
/// Persistence contract for client application entities.
/// </summary>
public interface IClientAppRepository
{
    Task<ClientApp?> FindByClientIdAsync(string clientId);
    Task AddAsync(ClientApp app);
}