using AuthService.Application.Features.Authentication.Commands.ClientCredentials;
using AuthService.Infrastructure.Security;
using HydroEspinaca.Shared.Extensions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;

namespace AuthService.Web;

public static class ServiceCollectionWebExtensions
{
    public static IServiceCollection AddWebApi(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment? environment = null)
    {
        // Use shared microservice configuration (includes Controllers, Auth, Swagger, HealthChecks)
        services.AddHydroEspinacaMicroservice(
            configuration,
            "auth-service",
            "Auth Service API",
            typeof(ClientCredentialsCommandValidator).Assembly
        );
        
        // Override JWT configuration for auth-service specific key resolution
        services.Configure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
        {
            var jwtSettings = configuration
                .GetSection("Jwt")
                .Get<AuthService.Infrastructure.Security.JwtSettings>()
                ?? throw new InvalidOperationException("Missing Jwt section in config");

            options.TokenValidationParameters.IssuerSigningKeyResolver = (token, securityToken, kid, validationParameters) =>
            {
                using var scope = services.BuildServiceProvider().CreateScope();
                var keyStore = scope.ServiceProvider.GetRequiredService<IKeyStore>();
                return ResolveSigningKeys(keyStore, kid, securityToken);
            };
        });

        return services;
    }

    /// <summary>
    /// Resolves signing keys based on KID from JWT token header
    /// </summary>
    private static IEnumerable<SecurityKey> ResolveSigningKeys(IKeyStore keyStore, string? kid, SecurityToken? securityToken)
    {
        // Strategy 1: Use KID if provided in parameters
        if (!string.IsNullOrEmpty(kid))
        {
            var keyPair = keyStore.GetKeyPairById(kid);
            if (keyPair != null)
            {
                var rsa = RSA.Create();
                rsa.ImportFromPem(keyPair.PublicKey.ToCharArray());
                return new[] { new RsaSecurityKey(rsa) { KeyId = kid } };
            }
        }
        
        // Strategy 2: Extract KID from JWT token header if not provided in parameters
        if (securityToken is JwtSecurityToken jwtToken && !string.IsNullOrEmpty(jwtToken.Header.Kid))
        {
            var keyPair = keyStore.GetKeyPairById(jwtToken.Header.Kid);
            if (keyPair != null)
            {
                var rsa = RSA.Create();
                rsa.ImportFromPem(keyPair.PublicKey.ToCharArray());
                return new[] { new RsaSecurityKey(rsa) { KeyId = jwtToken.Header.Kid } };
            }
        }
        
        // Strategy 3: Legacy fallback - return all available keys for tokens without KID
        // This allows validation of older tokens that don't have KID in the header
        var allKeys = new List<SecurityKey>();
        
        try
        {
            var allKeyPairs = keyStore.GetAllKeyPairs();
            foreach (var keyPair in allKeyPairs)
            {
                var rsa = RSA.Create();
                rsa.ImportFromPem(keyPair.PublicKey.ToCharArray());
                allKeys.Add(new RsaSecurityKey(rsa) { KeyId = keyPair.KeyId });
            }
            
            // If we have keys, return them
            if (allKeys.Count > 0)
            {
                return allKeys;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error resolving signing keys: {ex.Message}");
        }
        
        // Strategy 4: Final fallback - return empty collection (will cause validation to fail)
        return new SecurityKey[0];
    }

}
