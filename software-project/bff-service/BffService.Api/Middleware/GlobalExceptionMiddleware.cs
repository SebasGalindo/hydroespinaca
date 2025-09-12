using HydroEspinaca.Shared.Authentication.Interfaces;
using System.Net.Mime;

namespace BffService.Api.Middleware;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;
    private readonly IWebHostEnvironment _env;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger, IWebHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Unhandled exception in BFF Service");

            var exceptionMapper = context.RequestServices.GetService<IExceptionToProblemDetailsMapper>();
            
            if (exceptionMapper != null && exceptionMapper.CanHandle(ex))
            {
                var statusCode = exceptionMapper.GetStatusCode(ex);
                var problemDetails = exceptionMapper.MapToProblemDetails(ex, context.Request.Path, _env.IsDevelopment());

                context.Response.StatusCode = statusCode;
                context.Response.ContentType = MediaTypeNames.Application.Json;

                await context.Response.WriteAsJsonAsync(problemDetails);
            }
            else
            {
                // Fallback to default error response
                context.Response.StatusCode = 500;
                context.Response.ContentType = MediaTypeNames.Application.Json;
                
                await context.Response.WriteAsJsonAsync(new
                {
                    type = "https://tools.ietf.org/html/rfc9110#section-15.6.1",
                    title = "Internal Server Error",
                    status = 500,
                    detail = _env.IsDevelopment() ? ex.Message : "An error occurred in BFF service",
                    instance = context.Request.Path.ToString()
                });
            }
        }
    }
}