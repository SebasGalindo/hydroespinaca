namespace HydroEspinaca.Shared.Options;

public class JwtSettings
{
    public string Secret { get; set; } = default!;
    public int ExpirationInMinutes { get; set; } = 60;
}
