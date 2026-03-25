using HydroEspinaca.Shared.Authentication.Interfaces;
using System.Net.Mime;

namespace ChatbotService.Api.Middleware;

/// <summary>
/// Intercepta las excepciones lanzadas dentro de los endpoints, handlers y providers.
/// Las mapea usando el patrón de IExceptionToProblemDetailsMapper hacia la especificación RFC ProblemDetails.
/// </summary>
public class GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger, IWebHostEnvironment env)
{
    private readonly RequestDelegate _next = next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger = logger;
    private readonly IWebHostEnvironment _env = env;

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Unhandled exception in Chatbot Service");

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
                    detail = _env.IsDevelopment() ? ex.Message : "An error occurred in Chatbot service",
                    instance = context.Request.Path.ToString()
                });
            }
        }
    }
}
