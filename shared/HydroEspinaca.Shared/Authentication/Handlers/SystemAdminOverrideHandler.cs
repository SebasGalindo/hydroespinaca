using HydroEspinaca.Shared.Enums;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace HydroEspinaca.Shared.Authentication.Handlers;

/// <summary>
/// Authorization handler that provides global bypass for users with system:admin scope.
/// Any user with system:admin scope will automatically pass all authorization requirements.
/// This handler is designed to be reusable across all microservices in the HydroEspinaca system.
/// </summary>
public class SystemAdminOverrideHandler : IAuthorizationHandler
{
    public Task HandleAsync(AuthorizationHandlerContext context)
    {
        // Check if user has system:admin scope
        if (HasSystemAdminScope(context.User))
        {
            // Mark all pending requirements as succeeded
            foreach (var requirement in context.PendingRequirements.ToList())
            {
                context.Succeed(requirement);
            }
        }

        // Always return completed task - other handlers may still run for non-admin users
        return Task.CompletedTask;
    }

    /// <summary>
    /// Checks if the user has system:admin scope in their claims
    /// </summary>
    /// <param name="user">The claims principal to check</param>
    /// <returns>True if the user has system:admin scope, false otherwise</returns>
    public static bool HasSystemAdminScope(ClaimsPrincipal user)
    {
        // Get all scope claims (there might be multiple scope claims or one with space-separated values)
        var scopeClaims = user.FindAll("scope");
        
        foreach (var scopeClaim in scopeClaims)
        {
            if (string.IsNullOrEmpty(scopeClaim.Value))
                continue;

            // Handle space-separated scopes (OAuth 2.0 standard)
            var scopes = scopeClaim.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            
            if (scopes.Contains(AuthorizationScopes.SystemAdmin))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Checks if the user has any of the specified scopes
    /// </summary>
    /// <param name="user">The claims principal to check</param>
    /// <param name="requiredScopes">The scopes to check for</param>
    /// <returns>True if the user has any of the required scopes</returns>
    public static bool HasAnyScope(ClaimsPrincipal user, params string[] requiredScopes)
    {
        if (requiredScopes == null || requiredScopes.Length == 0)
            return true;

        var scopeClaims = user.FindAll("scope");
        
        foreach (var scopeClaim in scopeClaims)
        {
            if (string.IsNullOrEmpty(scopeClaim.Value))
                continue;

            var userScopes = scopeClaim.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            
            // Check if user has any of the required scopes
            if (requiredScopes.Any(required => userScopes.Contains(required)))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Checks if the user has all of the specified scopes
    /// </summary>
    /// <param name="user">The claims principal to check</param>
    /// <param name="requiredScopes">The scopes to check for</param>
    /// <returns>True if the user has all of the required scopes</returns>
    public static bool HasAllScopes(ClaimsPrincipal user, params string[] requiredScopes)
    {
        if (requiredScopes == null || requiredScopes.Length == 0)
            return true;

        var scopeClaims = user.FindAll("scope");
        var userScopes = new HashSet<string>();
        
        foreach (var scopeClaim in scopeClaims)
        {
            if (string.IsNullOrEmpty(scopeClaim.Value))
                continue;

            var scopes = scopeClaim.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            foreach (var scope in scopes)
            {
                userScopes.Add(scope);
            }
        }

        // Check if user has all required scopes
        return requiredScopes.All(required => userScopes.Contains(required));
    }

    /// <summary>
    /// Gets all scopes for the given user
    /// </summary>
    /// <param name="user">The claims principal</param>
    /// <returns>Collection of all scopes for the user</returns>
    public static IEnumerable<string> GetUserScopes(ClaimsPrincipal user)
    {
        var scopeClaims = user.FindAll("scope");
        var scopes = new HashSet<string>();
        
        foreach (var scopeClaim in scopeClaims)
        {
            if (string.IsNullOrEmpty(scopeClaim.Value))
                continue;

            var tokenScopes = scopeClaim.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            foreach (var scope in tokenScopes)
            {
                scopes.Add(scope);
            }
        }

        return scopes;
    }
}