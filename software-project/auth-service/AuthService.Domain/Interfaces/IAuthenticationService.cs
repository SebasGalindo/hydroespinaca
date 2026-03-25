using AuthService.Domain.Entities;

namespace AuthService.Domain.Interfaces;
/// <summary>
/// Contract for the user authentication service that validates credentials and issues JWT tokens.
/// </summary>
public interface IAuthenticationService
{
    Task<User> AuthenticateAsync(string email, string plainPassword);
}