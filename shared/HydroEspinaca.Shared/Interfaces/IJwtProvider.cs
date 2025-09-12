namespace HydroEspinaca.Shared.Interfaces;

public interface IJwtProvider
{
    string GenerateToken(string userId);
    bool ValidateToken(string token);
}
