namespace HydroEspinaca.Shared.Options;

/// <summary>
/// Configuration options for Machine-to-Machine (M2M) authentication
/// </summary>
public class M2MAuthOptions
{
    /// <summary>
    /// Configuration section name
    /// </summary>
    public const string SectionName = "M2M";

    /// <summary>
    /// Client ID for M2M authentication
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// Client secret for M2M authentication
    /// </summary>
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>
    /// Auth service URL for token requests
    /// </summary>
    public string AuthServiceUrl { get; set; } = string.Empty;

    /// <summary>
    /// Token endpoint path (default: /api/auth/token)
    /// </summary>
    public string TokenEndpoint { get; set; } = "/api/auth/token";

    /// <summary>
    /// Token cache duration in minutes (default: 50 minutes)
    /// </summary>
    public int TokenCacheDurationMinutes { get; set; } = 50;

    /// <summary>
    /// Gets the full token endpoint URL
    /// </summary>
    public string GetTokenUrl() => $"{AuthServiceUrl.TrimEnd('/')}{TokenEndpoint}";

    /// <summary>
    /// Validates the configuration
    /// </summary>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(ClientId))
            throw new InvalidOperationException("M2M ClientId is required");
        
        if (string.IsNullOrWhiteSpace(ClientSecret))
            throw new InvalidOperationException("M2M ClientSecret is required");
        
        if (string.IsNullOrWhiteSpace(AuthServiceUrl))
            throw new InvalidOperationException("M2M AuthServiceUrl is required");
    }
}