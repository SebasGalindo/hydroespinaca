using HydroEspinaca.Shared.Authentication.Handlers;
using HydroEspinaca.Shared.Authentication.Interfaces;
using HydroEspinaca.Shared.Authentication.Services;
using HydroEspinaca.Shared.Errors;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace HydroEspinaca.Shared.Authentication.Extensions;

/// <summary>
/// Extension methods for configuring shared authentication and authorization services
/// </summary>
public static class AuthenticationServiceExtensions
{
    /// <summary>
    /// Adds shared authentication services to the service collection
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">Configuration containing JWT settings</param>
    /// <param name="configureJwt">Optional JWT configuration action</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddSharedAuthentication(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<JwtBearerOptions>? configureJwt = null)
    {
        // Register authentication services
        services.AddHttpClient<IJwtKeyResolver, JwtKeyResolver>();
        services.AddScoped<ITokenValidator, JwksHelper>();
        services.AddSingleton<IExceptionToProblemDetailsMapper, ProblemDetailsFactory>();

        // Configure JWT authentication
        var jwtSection = configuration.GetSection("Jwt");
        var issuer = jwtSection["Issuer"] ?? throw new InvalidOperationException("JWT Issuer not configured");
        var audience = jwtSection["Audience"] ?? throw new InvalidOperationException("JWT Audience not configured");

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = issuer,
                    ValidateAudience = true,
                    ValidAudience = audience,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ClockSkew = TimeSpan.FromSeconds(30),
                    RoleClaimType = "role",
                    NameClaimType = "sub",
                    
                    // Dynamic key resolution using shared service
                    IssuerSigningKeyResolver = (token, securityToken, kid, validationParameters) =>
                    {
                        var serviceProvider = services.BuildServiceProvider();
                        using var scope = serviceProvider.CreateScope();
                        var keyResolver = scope.ServiceProvider.GetRequiredService<IJwtKeyResolver>();
                        
                        return ResolveSigningKeysAsync(keyResolver, kid, securityToken, issuer).GetAwaiter().GetResult();
                    }
                };

                options.MapInboundClaims = false;

                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = context =>
                    {
                        if (context.Principal?.Identity is ClaimsIdentity identity)
                        {
                            var newIdentity = new ClaimsIdentity(
                                identity.Claims,
                                identity.AuthenticationType,
                                nameType: "sub",
                                roleType: "role"
                            );
                            
                            context.Principal = new ClaimsPrincipal(newIdentity);
                        }
                        return Task.CompletedTask;
                    }
                };

                // Apply additional configuration if provided
                configureJwt?.Invoke(options);
            });

        return services;
    }

    /// <summary>
    /// Adds shared authorization services including system admin override
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configureAuthorization">Optional authorization configuration action</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddSharedAuthorization(
        this IServiceCollection services,
        Action<AuthorizationOptions>? configureAuthorization = null)
    {
        services.AddAuthorization(options =>
        {
            options.DefaultPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();

            // Apply additional configuration if provided
            configureAuthorization?.Invoke(options);
        });

        // Register the system admin override handler
        services.AddSingleton<IAuthorizationHandler, SystemAdminOverrideHandler>();

        return services;
    }

    /// <summary>
    /// Adds both shared authentication and authorization services
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">Configuration containing JWT settings</param>
    /// <param name="configureJwt">Optional JWT configuration action</param>
    /// <param name="configureAuthorization">Optional authorization configuration action</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddSharedAuthServices(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<JwtBearerOptions>? configureJwt = null,
        Action<AuthorizationOptions>? configureAuthorization = null)
    {
        return services
            .AddSharedAuthentication(configuration, configureJwt)
            .AddSharedAuthorization(configureAuthorization);
    }

    /// <summary>
    /// Adds scope-based authorization policies
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="scopePolicies">Dictionary of policy names and required scopes</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddScopePolicies(
        this IServiceCollection services,
        Dictionary<string, string[]> scopePolicies)
    {
        services.AddAuthorization(options =>
        {
            foreach (var policy in scopePolicies)
            {
                options.AddPolicy(policy.Key, policyBuilder =>
                {
                    policyBuilder.RequireAuthenticatedUser();
                    policyBuilder.RequireAssertion(context =>
                        SystemAdminOverrideHandler.HasAnyScope(context.User, policy.Value));
                });
            }
        });

        return services;
    }

    /// <summary>
    /// Resolves signing keys asynchronously for JWT validation
    /// </summary>
    private static async Task<IEnumerable<SecurityKey>> ResolveSigningKeysAsync(
        IJwtKeyResolver keyResolver,
        string? kid,
        SecurityToken? securityToken,
        string issuer)
    {
        try
        {
            // Extract KID from JWT token header if not provided
            if (string.IsNullOrEmpty(kid) && securityToken is JwtSecurityToken jwtToken)
            {
                kid = jwtToken.Header.Kid;
            }

            var keys = await keyResolver.ResolveSigningKeysAsync(kid, issuer);
            return keys;
        }
        catch (Exception)
        {
            // Return empty collection on error - this will cause token validation to fail
            return Enumerable.Empty<SecurityKey>();
        }
    }
}

/// <summary>
/// Configuration options for shared authentication
/// </summary>
public class SharedAuthOptions
{
    /// <summary>
    /// JWT issuer URL
    /// </summary>
    public string Issuer { get; set; } = string.Empty;

    /// <summary>
    /// JWT audience
    /// </summary>
    public string Audience { get; set; } = string.Empty;

    /// <summary>
    /// Clock skew tolerance for token validation
    /// </summary>
    public TimeSpan ClockSkew { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// JWKS cache expiry time
    /// </summary>
    public TimeSpan JwksCacheExpiry { get; set; } = TimeSpan.FromMinutes(15);

    /// <summary>
    /// Whether to validate the token lifetime
    /// </summary>
    public bool ValidateLifetime { get; set; } = true;

    /// <summary>
    /// Whether to validate the issuer
    /// </summary>
    public bool ValidateIssuer { get; set; } = true;

    /// <summary>
    /// Whether to validate the audience
    /// </summary>
    public bool ValidateAudience { get; set; } = true;
}