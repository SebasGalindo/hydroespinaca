namespace BffService.Domain.Constants;

public static class BffConstants
{
    public static class Sessions
    {
        public const string SessionIdHeaderConfigKey = "Sessions:SessionIdHeader";
        public const string CsrfTokenHeaderConfigKey = "Sessions:CsrfTokenHeader";
        public const string AccessTokenExpiryMinutesConfigKey = "Sessions:AccessTokenExpiryMinutes";
        public const string RefreshTokenExpiryDaysConfigKey = "Sessions:RefreshTokenExpiryDays";
    }

    public static class Auth
    {
        public const string ClientIdConfigKey = "M2M:ClientId";
        public const string ClientSecretConfigKey = "M2M:ClientSecret";
        public const string AuthServiceUrlConfigKey = "M2M:AuthServiceUrl";
        public const string TokenEndpointConfigKey = "M2M:TokenEndpoint";
        public const string IssuerConfigKey = "Jwt:Issuer";
        public const string AudienceConfigKey = "Jwt:Audience";
    }

    public static class Proxy
    {
        public const string ProxyBasePath = "/proxy";
        
        public static class Services
        {
            public const string SensorService = "sensor-service";
            public const string ActuatorService = "actuator-service";
            public const string AuthService = "auth-service";
        }

        public static readonly Dictionary<string, string> ServiceRoutes = new()
        {
            { "/sensor", Services.SensorService },
            { "/actuator", Services.ActuatorService },
            { "/auth", Services.AuthService }
        };

        // Public routes that don't require authentication
        public static readonly HashSet<string> PublicRoutes = new(StringComparer.OrdinalIgnoreCase)
        {
            "/proxy/auth/api/auth/keys/public",
            "/proxy/auth/api/auth/login",
            "/proxy/auth/api/auth/token"
        };
    }

    public static class Headers
    {
        public const string Authorization = "Authorization";
        public const string ContentType = "Content-Type";
        public const string UserAgent = "User-Agent";
        public const string XForwardedFor = "X-Forwarded-For";
        public const string XRealIp = "X-Real-IP";
    }
}