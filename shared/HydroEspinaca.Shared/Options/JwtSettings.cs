namespace HydroEspinaca.Shared.Options;

public class JwtSettings
{
    public required string PrivateKeyPath { get; set; }
    public required string PublicKeyPath { get; set; }
    public required string Issuer { get; set; } 
    public required string Audience { get; set; }
    public required int AccessTokenExpiryMinutes { get; set; }
}