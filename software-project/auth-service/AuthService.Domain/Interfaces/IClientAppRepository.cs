using AuthService.Domain.Entities;

namespace AuthService.Domain.Interfaces;
public interface IClientAppRepository
{
    Task<ClientApp?> FindByClientIdAsync(string clientId);
    Task AddAsync(ClientApp app);
}