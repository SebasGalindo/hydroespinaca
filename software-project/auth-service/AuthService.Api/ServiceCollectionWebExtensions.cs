using AuthService.Api.Authorization;
using AuthService.Application.Features.Authentication.Commands.ClientCredentials;
using AuthService.Domain.Enums;
using AuthService.Infrastructure.Security;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;

namespace AuthService.Web;

public static class ServiceCollectionWebExtensions
{
    public static IServiceCollection AddWebApi(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment? environment = null)
    {

        var jwtSettings = configuration
            .GetSection("Jwt")
            .Get<AuthService.Infrastructure.Security.JwtSettings>()
            ?? throw new InvalidOperationException("Missing Jwt section in config");


        var aspNetCoreEnv = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        var hostEnv = environment?.EnvironmentName;
        var isTestEnvironment = aspNetCoreEnv == "Test" || hostEnv == "Test";

        Console.WriteLine($"Environment: {aspNetCoreEnv ?? hostEnv ?? "Unknown"}");

        services.AddControllers();

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(opts =>
            {
                opts.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwtSettings.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwtSettings.Audience,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ClockSkew = TimeSpan.FromSeconds(30),
                    RoleClaimType = "role",
                    NameClaimType = "sub",
                    
                    IssuerSigningKeyResolver = (token, securityToken, kid, validationParameters) =>
                    {
                        using var scope = services.BuildServiceProvider().CreateScope();
                        var keyStore = scope.ServiceProvider.GetRequiredService<IKeyStore>();
                        
                        return ResolveSigningKeys(keyStore, kid, securityToken);
                    }
                };

                opts.MapInboundClaims = false;

                opts.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var token = context.Token;
                        var authHeader = context.Request.Headers.Authorization.FirstOrDefault();
                        
                        return Task.CompletedTask;
                    },
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
                            
                            var roleClaims = newIdentity.FindAll("role").ToList();
                            var jwtToken = context.SecurityToken as JwtSecurityToken;
                        }
                        return Task.CompletedTask;
                    },
                    OnAuthenticationFailed = context =>
                    {
                        return Task.CompletedTask;
                    },
                    OnChallenge = context =>
                    {
                        return Task.CompletedTask;
                    }
                };
            });

        services.AddAuthorization(options =>
        {
            options.DefaultPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();
                
            // Configure scope-based authorization policies
            AuthorizationPolicies.ConfigurePolicies(options);
        });

        // Register the system admin override handler for global bypass
        services.AddSingleton<IAuthorizationHandler, SystemAdminOverrideHandler>();

        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
        {
            // Generate dynamic scope documentation
            var scopeDocumentation = GenerateScopeDocumentation();
            
            c.SwaggerDoc("v1", new OpenApiInfo 
            { 
                Title = "Auth Service API", 
                Version = "v1",
                Description = "Authentication service with scope-based authorization. Each endpoint requires specific scopes in the JWT token.\n\n" + scopeDocumentation
            });

            var securityScheme = new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "JWT Authorization header using the Bearer scheme. Swagger will automatically add 'Bearer ' prefix to your token."
            };

            c.AddSecurityDefinition("Bearer", securityScheme);

            var securityRequirement = new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            };

            c.AddSecurityRequirement(securityRequirement);
            
            // Enable XML comments if available
            var xmlFilename = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFilename);
            if (File.Exists(xmlPath))
            {
                c.IncludeXmlComments(xmlPath);
            }
        });

        services.AddValidatorsFromAssemblyContaining<ClientCredentialsCommandValidator>();
        services.AddHealthChecks();

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

    /// <summary>
    /// Generates dynamic scope documentation from the AuthorizationScopes enum
    /// </summary>
    private static string GenerateScopeDocumentation()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("## Available Scopes\n");
        
        // Get all scope constants from the enum using reflection
        var scopeType = typeof(HydroEspinaca.Shared.Enums.AuthorizationScopes);
        var scopeFields = scopeType.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.FlattenHierarchy)
                                  .Where(f => f.IsLiteral && !f.IsInitOnly && f.FieldType == typeof(string))
                                  .OrderBy(f => f.Name);

        // Group scopes by category
        var scopeGroups = new Dictionary<string, List<(string Name, string Value)>>
        {
            ["User Management"] = new(),
            ["Profile Management"] = new(),
            ["Role Management"] = new(),
            ["Permission Management"] = new(),
            ["IoT Hardware"] = new(),
            ["Data Management"] = new(),
            ["System Operations"] = new()
        };

        foreach (var field in scopeFields)
        {
            var scopeName = field.Name;
            var scopeValue = field.GetValue(null)?.ToString() ?? "";
            
            // Categorize scopes based on their name
            if (scopeName.StartsWith("User"))
                scopeGroups["User Management"].Add((scopeName, scopeValue));
            else if (scopeName.StartsWith("Profile"))
                scopeGroups["Profile Management"].Add((scopeName, scopeValue));
            else if (scopeName.StartsWith("Role"))
                scopeGroups["Role Management"].Add((scopeName, scopeValue));
            else if (scopeName.StartsWith("Permission"))
                scopeGroups["Permission Management"].Add((scopeName, scopeValue));
            else if (scopeName.StartsWith("Sensor") || scopeName.StartsWith("Actuator") || scopeName.StartsWith("Esp32"))
                scopeGroups["IoT Hardware"].Add((scopeName, scopeValue));
            else if (scopeName.StartsWith("Variable") || scopeName.StartsWith("Alert"))
                scopeGroups["Data Management"].Add((scopeName, scopeValue));
            else if (scopeName.StartsWith("System"))
                scopeGroups["System Operations"].Add((scopeName, scopeValue));
        }

        // Generate documentation for each category
        foreach (var group in scopeGroups.Where(g => g.Value.Any()))
        {
            sb.AppendLine($"### {group.Key}");
            foreach (var scope in group.Value)
            {
                sb.AppendLine($"- `{scope.Value}` - {GetScopeDescription(scope.Name, scope.Value)}");
            }
            sb.AppendLine();
        }

        return sb.ToString();
    }

    /// <summary>
    /// Gets a human-readable description for a scope
    /// </summary>
    private static string GetScopeDescription(string scopeName, string scopeValue)
    {
        return scopeName switch
        {
            "UserCreate" => "Create new users",
            "UserRead" => "Read user information",
            "UserUpdate" => "Update user information", 
            "UserDelete" => "Delete users",
            "ProfileRead" => "Read own profile information",
            "ProfileUpdate" => "Update own profile information",
            "RoleCreate" => "Create new roles",
            "RoleRead" => "Read role information",
            "RoleUpdate" => "Update role information",
            "RoleDelete" => "Delete roles",
            "PermissionCreate" => "Create new permissions",
            "PermissionRead" => "Read permission information",
            "PermissionUpdate" => "Update permission information",
            "PermissionDelete" => "Delete permissions",
            "SensorRead" => "Read sensor data and status",
            "SensorWrite" => "Write sensor data and configuration",
            "ActuatorRead" => "Read actuator status and information",
            "ActuatorControl" => "Control actuator operations",
            "Esp32Read" => "Read ESP32 node information",
            "Esp32Write" => "Write ESP32 node configuration",
            "Esp32Control" => "Control ESP32 node operations",
            "VariableRead" => "Read variable information",
            "VariableWrite" => "Write variable data and configuration",
            "AlertRead" => "Read alert information",
            "AlertWrite" => "Write alert data and configuration",
            "AlertManage" => "Manage alert rules and configuration",
            "SystemAdmin" => "System administration access",
            "SystemHealth" => "Access system health information",
            "SystemMonitor" => "Monitor system operations",
            _ => $"Access to {scopeValue.Replace(":", " ")} operations"
        };
    }
}
