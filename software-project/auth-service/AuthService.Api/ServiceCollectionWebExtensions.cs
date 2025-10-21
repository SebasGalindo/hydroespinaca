using AuthService.Application.Features.Authentication.Commands.ClientCredentials;
using AuthService.Domain.Settings;
using AuthService.Infrastructure.Security;
using HydroEspinaca.Shared.Extensions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
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
        services.AddHydroEspinacaMicroservice(
            configuration,
            "auth-service",
            "Auth Service API",
            typeof(ClientCredentialsCommandValidator).Assembly
        );
        
        services.ConfigureOptions<ConfigureJwtBearerOptions>();

        return services;
    }

    private class ConfigureJwtBearerOptions : IConfigureNamedOptions<JwtBearerOptions>
    {
        private readonly IServiceProvider _serviceProvider;

        public ConfigureJwtBearerOptions(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public void Configure(string? name, JwtBearerOptions options)
        {
            if (name == JwtBearerDefaults.AuthenticationScheme)
            {
                Configure(options);
            }
        }

        public void Configure(JwtBearerOptions options)
        {
            options.TokenValidationParameters.IssuerSigningKeyResolver = (token, securityToken, kid, validationParameters) =>
            {
                using var scope = _serviceProvider.CreateScope();
                var keyStore = scope.ServiceProvider.GetRequiredService<IKeyStore>();
                return ResolveSigningKeys(keyStore, kid, securityToken);
            };
        }
    }

    /// <summary>
    /// Resolves signing keys based on KID from JWT token header.
    /// Supports multi-key rotation strategy using KID header.
    /// </summary>
    private static IEnumerable<SecurityKey> ResolveSigningKeys(IKeyStore keyStore, string? kid, SecurityToken? securityToken)
    {
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
        
        var allKeys = new List<SecurityKey>();
        var allKeyPairs = keyStore.GetAllKeyPairs();
        foreach (var keyPair in allKeyPairs)
        {
            var rsa = RSA.Create();
            rsa.ImportFromPem(keyPair.PublicKey.ToCharArray());
            allKeys.Add(new RsaSecurityKey(rsa) { KeyId = keyPair.KeyId });
        }
        
        return allKeys.Count > 0 ? allKeys : Array.Empty<SecurityKey>();
    }
}
