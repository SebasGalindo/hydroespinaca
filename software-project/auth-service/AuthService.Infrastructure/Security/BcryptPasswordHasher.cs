using AuthService.Domain.Interfaces;

namespace AuthService.Infrastructure.Security;
public class BcryptPasswordHasher : IPasswordHasher
{
    public string Hash(string password)
        => BCrypt.Net.BCrypt.HashPassword(password);

    public bool Verify(string hashedPassword, string providedPassword)
        => BCrypt.Net.BCrypt.Verify(providedPassword, hashedPassword);
}