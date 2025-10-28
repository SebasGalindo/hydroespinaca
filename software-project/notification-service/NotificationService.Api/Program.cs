using NotificationService.Api.Extensions;
using NotificationService.Infrastructure;
using NotificationService.Application;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using NotificationService.Api.HealthChecks;
using NotificationService.Api.Middleware;
using HydroEspinaca.Shared.Authentication.Interfaces;
using HydroEspinaca.Shared.Errors;

var builder = WebApplication.CreateBuilder(args);

// Configure logging
builder.Logging.ClearProviders();
builder.Logging.AddSimpleConsole(options =>
{
    options.IncludeScopes = false;
    options.SingleLine = true;
    options.TimestampFormat = "yyyy-MM-dd HH:mm:ss ";
});

// ✅ Configure Clean Architecture layers
builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddApplicationServices();
builder.Services.AddNotificationServiceApi(builder.Configuration, builder.Environment);

// ✅ CRITICAL: Register the shared and specific exception mappers
builder.Services.AddSingleton<HydroEspinaca.Shared.Errors.ProblemDetailsFactory>();
builder.Services.AddSingleton<IExceptionToProblemDetailsMapper, NotificationService.Api.Services.NotificationServiceExceptionMapper>();

// Add specific health checks
builder.Services.AddHealthChecks()
    .AddCheck<EmailProvidersHealthCheck>("email_providers");

var app = builder.Build();

// Configure middleware pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Notification Service API v1")
    );
}

// ⚠️ IMPORTANTE: Orden de middleware (crítico para seguridad y manejo de errores)
// 1. GlobalExceptionMiddleware - DEBE ir primero para capturar todas las excepciones
// 2. Authentication - Validar y decodificar JWT
// 3. Authorization - Verificar scopes/policies (depende de Authentication)
app.UseMiddleware<NotificationService.Api.Middleware.GlobalExceptionMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health").AllowAnonymous();
app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = (check) => true,
    ResultStatusCodes =
    {
        [HealthStatus.Healthy] = StatusCodes.Status200OK,
        [HealthStatus.Degraded] = StatusCodes.Status200OK,
        [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable,
    }
}).AllowAnonymous();

await app.RunAsync();

public partial class Program { } // For integration tests