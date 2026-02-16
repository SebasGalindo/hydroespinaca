namespace AuthService.Domain.Interfaces;
/// <summary>
/// Contract for password hashing and verification operations.
/// </summary>
public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string hashedPassword, string providedPassword);
}
