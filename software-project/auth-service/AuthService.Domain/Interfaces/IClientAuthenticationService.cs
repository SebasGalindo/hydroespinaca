using AuthService.Domain.Entities;

namespace AuthService.Domain.Interfaces;
/// <summary>
/// Contract for authenticating client applications using client credentials flow.
/// </summary>
public interface IClientAuthenticationService
{
    Task<ClientApp> AuthenticateClientAsync(string clientId, string clientSecret);
}
