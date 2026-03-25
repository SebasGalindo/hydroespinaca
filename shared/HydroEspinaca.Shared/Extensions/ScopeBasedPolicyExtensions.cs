using HydroEspinaca.Shared.Authentication.Handlers;
using HydroEspinaca.Shared.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace HydroEspinaca.Shared.Extensions;

/// <summary>
/// Extension methods for configuring scope-based authorization policies using AuthorizationScopes constants
/// </summary>
public static class ScopeBasedPolicyExtensions
{
    /// <summary>
    /// Adds individual scope-based policies for each scope in AuthorizationScopes
    /// This allows using AuthorizationScopes.UserRead directly in [Authorize(Policy = AuthorizationScopes.UserRead)]
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddIndividualScopePolicies(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            // Get all scope constants from AuthorizationScopes using reflection
            var scopeFields = typeof(AuthorizationScopes)
                .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
                .Where(f => f.IsLiteral && !f.IsInitOnly && f.FieldType == typeof(string));

            foreach (var field in scopeFields)
            {
                var scopeName = field.Name;
                var scopeValue = field.GetValue(null)?.ToString();
                
                if (!string.IsNullOrEmpty(scopeValue))
                {
                    // Create a policy for each individual scope
                    // Policy name matches the constant name (e.g., "UserRead", "UserCreate")
                    options.AddPolicy(scopeName, policy =>
                    {
                        policy.RequireAuthenticatedUser();
                        policy.RequireAssertion(context =>
                            SystemAdminOverrideHandler.HasAnyScope(context.User, scopeValue));
                    });
                }
            }
        });

        return services;
    }

    /// <summary>
    /// Adds both grouped policies (like "UserWrite") and individual scope policies
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="additionalPolicies">Additional service-specific policies</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddCompleteScopePolicies(
        this IServiceCollection services,
        Dictionary<string, string[]>? additionalPolicies = null)
    {
        // Add individual scope policies
        services.AddIndividualScopePolicies();
        
        // Add grouped policies for convenience
        services.AddStandardScopePolicies(additionalPolicies);

        return services;
    }

    /// <summary>
    /// Creates a policy that requires ANY of the specified scopes
    /// </summary>
    /// <param name="options">Authorization options</param>
    /// <param name="policyName">Name of the policy</param>
    /// <param name="requiredScopes">Scopes that grant access to this policy</param>
    public static void AddScopePolicy(this AuthorizationOptions options, string policyName, params string[] requiredScopes)
    {
        options.AddPolicy(policyName, policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.RequireAssertion(context =>
                SystemAdminOverrideHandler.HasAnyScope(context.User, requiredScopes));
        });
    }

    /// <summary>
    /// Creates a policy that requires ALL of the specified scopes
    /// </summary>
    /// <param name="options">Authorization options</param>
    /// <param name="policyName">Name of the policy</param>
    /// <param name="requiredScopes">All scopes required for access</param>
    public static void AddStrictScopePolicy(this AuthorizationOptions options, string policyName, params string[] requiredScopes)
    {
        options.AddPolicy(policyName, policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.RequireAssertion(context =>
                SystemAdminOverrideHandler.HasAllScopes(context.User, requiredScopes));
        });
    }
}

/// <summary>
/// Policy name constants that map directly to AuthorizationScopes for use in controllers
/// This provides IntelliSense and compile-time checking for policy names
/// </summary>
public static class PolicyNames
{
    // User management policies
    public const string UserRead = nameof(AuthorizationScopes.UserRead);
    public const string UserCreate = nameof(AuthorizationScopes.UserCreate);
    public const string UserUpdate = nameof(AuthorizationScopes.UserUpdate);
    public const string UserDelete = nameof(AuthorizationScopes.UserDelete);
    
    // Profile management policies
    public const string ProfileRead = nameof(AuthorizationScopes.ProfileRead);
    public const string ProfileUpdate = nameof(AuthorizationScopes.ProfileUpdate);
    
    // Role management policies
    public const string RoleRead = nameof(AuthorizationScopes.RoleRead);
    public const string RoleCreate = nameof(AuthorizationScopes.RoleCreate);
    public const string RoleUpdate = nameof(AuthorizationScopes.RoleUpdate);
    public const string RoleDelete = nameof(AuthorizationScopes.RoleDelete);
    
    // Permission management policies
    public const string PermissionRead = nameof(AuthorizationScopes.PermissionRead);
    public const string PermissionCreate = nameof(AuthorizationScopes.PermissionCreate);
    public const string PermissionUpdate = nameof(AuthorizationScopes.PermissionUpdate);
    public const string PermissionDelete = nameof(AuthorizationScopes.PermissionDelete);
    
    // Hardware management policies
    public const string SensorRead = nameof(AuthorizationScopes.SensorRead);
    public const string SensorCreate = nameof(AuthorizationScopes.SensorCreate);
    public const string SensorUpdate = nameof(AuthorizationScopes.SensorUpdate);
    public const string SensorDelete = nameof(AuthorizationScopes.SensorDelete);
    public const string SensorWrite = nameof(AuthorizationScopes.SensorWrite);
    
    // Reading management policies
    public const string ReadingRead = nameof(AuthorizationScopes.ReadingRead);
    public const string ReadingCreate = nameof(AuthorizationScopes.ReadingCreate);
    
    // Aggregate data policies
    public const string AggregateRead = nameof(AuthorizationScopes.AggregateRead);
    
    // ESP32 Alert management policies
    public const string Esp32AlertRead = nameof(AuthorizationScopes.Esp32AlertRead);
    public const string Esp32AlertWrite = nameof(AuthorizationScopes.Esp32AlertWrite);
    
    public const string ActuatorRead = nameof(AuthorizationScopes.ActuatorRead);
    public const string ActuatorCreate = nameof(AuthorizationScopes.ActuatorCreate);
    public const string ActuatorUpdate = nameof(AuthorizationScopes.ActuatorUpdate);
    public const string ActuatorDelete = nameof(AuthorizationScopes.ActuatorDelete);
    public const string ActuatorControl = nameof(AuthorizationScopes.ActuatorControl);
    
    // Command management policies
    public const string CommandRead = nameof(AuthorizationScopes.CommandRead);
    public const string CommandCreate = nameof(AuthorizationScopes.CommandCreate);
    
    // ESP32 management policies
    public const string Esp32Read = nameof(AuthorizationScopes.Esp32Read);
    public const string Esp32Write = nameof(AuthorizationScopes.Esp32Write);
    public const string Esp32Control = nameof(AuthorizationScopes.Esp32Control);
    
    // Variable management policies
    public const string VariableRead = nameof(AuthorizationScopes.VariableRead);
    public const string VariableWrite = nameof(AuthorizationScopes.VariableWrite);
    
    // Alert management policies
    public const string AlertRead = nameof(AuthorizationScopes.AlertRead);
    public const string AlertWrite = nameof(AuthorizationScopes.AlertWrite);
    public const string AlertManage = nameof(AuthorizationScopes.AlertManage);
    
    // Notification management policies
    public const string NotificationSend = nameof(AuthorizationScopes.NotificationSend);
    public const string NotificationRead = nameof(AuthorizationScopes.NotificationRead);
    public const string NotificationManage = nameof(AuthorizationScopes.NotificationManage);
    public const string NotificationDiagnostics = nameof(AuthorizationScopes.NotificationDiagnostics);
    
    // System policies
    public const string SystemAdmin = nameof(AuthorizationScopes.SystemAdmin);
    public const string SystemHealth = nameof(AuthorizationScopes.SystemHealth);
    public const string SystemMonitor = nameof(AuthorizationScopes.SystemMonitor);
    
    // Weather policies
    public const string WeatherRead = nameof(AuthorizationScopes.WeatherRead);
    public const string WeatherWrite = nameof(AuthorizationScopes.WeatherWrite);

    // BI policies
    public const string BiRead = nameof(AuthorizationScopes.BiRead);
    public const string BiWrite = nameof(AuthorizationScopes.BiWrite);

    /// <summary>
    /// Grouped policies for convenience (multiple scopes)
    /// </summary>
    public static class Grouped
    {
        public const string UserWrite = "UserWrite";          // UserCreate + UserUpdate + UserDelete
        public const string RoleWrite = "RoleWrite";          // RoleCreate + RoleUpdate + RoleDelete  
        public const string PermissionWrite = "PermissionWrite"; // PermissionCreate + PermissionUpdate + PermissionDelete
        public const string UserManagement = "UserManagement"; // All user + role + permission scopes
        public const string HardwareRead = "HardwareRead";    // All read scopes for hardware
        public const string HardwareWrite = "HardwareWrite";  // All write/control scopes for hardware
        public const string NotificationFull = "NotificationFull"; // All notification scopes
    }
}