using AuthService.Domain.Entities;

namespace AuthService.Domain.Interfaces;
public interface IAuthenticationService
{
    Task<User> AuthenticateAsync(string email, string plainPassword);
}