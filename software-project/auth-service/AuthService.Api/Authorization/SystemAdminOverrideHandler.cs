using HydroEspinaca.Shared.Enums;
using Microsoft.AspNetCore.Authorization;

namespace AuthService.Api.Authorization;

/// <summary>
/// Authorization handler that provides global bypass for users with system:admin scope.
/// Any user with system:admin scope will automatically pass all authorization requirements.
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
    private static bool HasSystemAdminScope(System.Security.Claims.ClaimsPrincipal user)
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
}