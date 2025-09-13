using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace BffService.Api.Extensions;

/// <summary>
/// Operation filter to add session and CSRF headers to Swagger UI for BFF endpoints
/// </summary>
public class SessionHeadersOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var controllerName = context.MethodInfo.DeclaringType?.Name ?? "";
        var actionName = context.MethodInfo.Name;
        
        // Add headers to relevant endpoints
        var isProxyController = controllerName.Contains("ProxyController");
        var isAuthController = controllerName.Contains("AuthController");
        var isLoginEndpoint = isAuthController && actionName.Contains("Login");
        
        if (!isProxyController && !isAuthController)
        {
            return;
        }

        operation.Parameters ??= new List<OpenApiParameter>();

        // Don't add session headers to login endpoint (it creates the session)
        if (!isLoginEndpoint)
        {
            // Add X-Session-Id header
            operation.Parameters.Add(new OpenApiParameter
            {
                Name = "X-Session-Id",
                In = ParameterLocation.Header,
                Required = false,
                Description = "Session ID obtained from login endpoint. Required for authenticated requests.",
                Schema = new OpenApiSchema
                {
                    Type = "string"
                }
            });
        }

        // Add X-CSRF-Token header for state-changing operations (excluding login)
        var isStateChanging = actionName.Contains("Post") || 
                            actionName.Contains("Put") || 
                            actionName.Contains("Delete") || 
                            actionName.Contains("Patch");

        if (isStateChanging && !isLoginEndpoint)
        {
            operation.Parameters.Add(new OpenApiParameter
            {
                Name = "X-CSRF-Token",
                In = ParameterLocation.Header,
                Required = false,
                Description = "CSRF token from session info. Required for state-changing operations (POST, PUT, DELETE, PATCH).",
                Schema = new OpenApiSchema
                {
                    Type = "string"
                }
            });
        }
    }
}