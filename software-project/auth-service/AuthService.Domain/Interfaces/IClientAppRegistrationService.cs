namespace AuthService.Domain.Interfaces;
/// <summary>
/// Contract for registering and managing client applications for M2M authentication.
/// </summary>
public interface IClientAppRegistrationService
{
    Task RegisterAsync(string clientId, string secretPlain, IEnumerable<string> scopes);
}