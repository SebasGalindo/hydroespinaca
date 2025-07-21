using HydroEspinaca.Shared.Interfaces;

namespace HydroEspinaca.Shared.Utils;

public class JwtProvider : IJwtProvider
{
    public string GenerateToken(string userId)
    {
        // Implement token generation logic here
        return "token";
    }

    public bool ValidateToken(string token)
    {
        // Implement token validation logic here
        return true;
    }
}
