using AuthService.Domain.Interfaces;

namespace AuthService.Infrastructure.Security;
public class BcryptPasswordHasher : IPasswordHasher
{
    private const int WorkFactor = 11;

    public string Hash(string password)
    {
        try
        {
            // Use explicit parameters to ensure compatibility
            return BCrypt.Net.BCrypt.HashPassword(password, WorkFactor, false);
        }
        catch (Exception ex)
        {
            // Fallback: use enhanced entropy mode
            return BCrypt.Net.BCrypt.HashPassword(password, WorkFactor, true);
        }
    }

    public bool Verify(string hashedPassword, string providedPassword)
    {
        try
        {
            // BCrypt.Verify expects (password, hash) order
            return BCrypt.Net.BCrypt.Verify(providedPassword, hashedPassword, false);
        }
        catch (Exception)
        {
            try
            {
                // Try with enhanced entropy if standard fails
                return BCrypt.Net.BCrypt.Verify(providedPassword, hashedPassword, true);
            }
            catch (Exception)
            {
                // If all fails, return false
                return false;
            }
        }
    }
}