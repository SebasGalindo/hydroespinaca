namespace AuthService.Domain.Interfaces;
public interface IClientAppRegistrationService
{
    Task RegisterAsync(string clientId, string secretPlain, IEnumerable<string> scopes);
}