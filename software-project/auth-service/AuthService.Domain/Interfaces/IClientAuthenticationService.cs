using AuthService.Domain.Entities;

namespace AuthService.Domain.Interfaces;
public interface IClientAuthenticationService
{
    Task<ClientApp> AuthenticateClientAsync(string clientId, string clientSecret);
}
