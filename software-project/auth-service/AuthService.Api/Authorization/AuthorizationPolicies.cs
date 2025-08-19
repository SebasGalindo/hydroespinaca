using HydroEspinaca.Shared.Enums;
using Microsoft.AspNetCore.Authorization;

namespace AuthService.Api.Authorization;

/// <summary>
/// Defines all authorization policy names for scope-based authorization
/// </summary>
public static class AuthorizationPolicies
{
    // User management policies
    public const string RequireUserRead = "RequireUserRead";
    public const string RequireUserCreate = "RequireUserCreate";
    public const string RequireUserUpdate = "RequireUserUpdate";
    public const string RequireUserDelete = "RequireUserDelete";
    
    // Profile management policies
    public const string RequireProfileRead = "RequireProfileRead";
    public const string RequireProfileUpdate = "RequireProfileUpdate";
    
    // Role management policies
    public const string RequireRoleRead = "RequireRoleRead";
    public const string RequireRoleCreate = "RequireRoleCreate";
    public const string RequireRoleUpdate = "RequireRoleUpdate";
    public const string RequireRoleDelete = "RequireRoleDelete";
    
    // Permission management policies
    public const string RequirePermissionRead = "RequirePermissionRead";
    public const string RequirePermissionCreate = "RequirePermissionCreate";
    public const string RequirePermissionUpdate = "RequirePermissionUpdate";
    public const string RequirePermissionDelete = "RequirePermissionDelete";
    
    // Machine-to-Machine policies
    public const string RequireSensorRead = "RequireSensorRead";
    public const string RequireSensorWrite = "RequireSensorWrite";
    public const string RequireActuatorRead = "RequireActuatorRead";
    public const string RequireActuatorControl = "RequireActuatorControl";
    
    // System-level policies
    public const string RequireSystemAdmin = "RequireSystemAdmin";
    public const string RequireSystemHealth = "RequireSystemHealth";
    
    /// <summary>
    /// Configures all authorization policies for the application
    /// </summary>
    public static void ConfigurePolicies(AuthorizationOptions options)
    {
        // User management policies
        options.AddPolicy(RequireUserRead, policy => 
            policy.RequireClaim("scope", AuthorizationScopes.UserRead));
        options.AddPolicy(RequireUserCreate, policy => 
            policy.RequireClaim("scope", AuthorizationScopes.UserCreate));
        options.AddPolicy(RequireUserUpdate, policy => 
            policy.RequireClaim("scope", AuthorizationScopes.UserUpdate));
        options.AddPolicy(RequireUserDelete, policy => 
            policy.RequireClaim("scope", AuthorizationScopes.UserDelete));
            
        // Profile management policies
        options.AddPolicy(RequireProfileRead, policy => 
            policy.RequireClaim("scope", AuthorizationScopes.ProfileRead));
        options.AddPolicy(RequireProfileUpdate, policy => 
            policy.RequireClaim("scope", AuthorizationScopes.ProfileUpdate));
            
        // Role management policies
        options.AddPolicy(RequireRoleRead, policy => 
            policy.RequireClaim("scope", AuthorizationScopes.RoleRead));
        options.AddPolicy(RequireRoleCreate, policy => 
            policy.RequireClaim("scope", AuthorizationScopes.RoleCreate));
        options.AddPolicy(RequireRoleUpdate, policy => 
            policy.RequireClaim("scope", AuthorizationScopes.RoleUpdate));
        options.AddPolicy(RequireRoleDelete, policy => 
            policy.RequireClaim("scope", AuthorizationScopes.RoleDelete));
            
        // Permission management policies
        options.AddPolicy(RequirePermissionRead, policy => 
            policy.RequireClaim("scope", AuthorizationScopes.PermissionRead));
        options.AddPolicy(RequirePermissionCreate, policy => 
            policy.RequireClaim("scope", AuthorizationScopes.PermissionCreate));
        options.AddPolicy(RequirePermissionUpdate, policy => 
            policy.RequireClaim("scope", AuthorizationScopes.PermissionUpdate));
        options.AddPolicy(RequirePermissionDelete, policy => 
            policy.RequireClaim("scope", AuthorizationScopes.PermissionDelete));
            
        // IoT Hardware policies
        options.AddPolicy(RequireSensorRead, policy => 
            policy.RequireClaim("scope", AuthorizationScopes.SensorRead));
        options.AddPolicy(RequireSensorWrite, policy => 
            policy.RequireClaim("scope", AuthorizationScopes.SensorWrite));
        options.AddPolicy(RequireActuatorRead, policy => 
            policy.RequireClaim("scope", AuthorizationScopes.ActuatorRead));
        options.AddPolicy(RequireActuatorControl, policy => 
            policy.RequireClaim("scope", AuthorizationScopes.ActuatorControl));
            
        // System-level policies
        options.AddPolicy(RequireSystemAdmin, policy => 
            policy.RequireClaim("scope", AuthorizationScopes.SystemAdmin));
        options.AddPolicy(RequireSystemHealth, policy => 
            policy.RequireClaim("scope", AuthorizationScopes.SystemHealth));
    }
}