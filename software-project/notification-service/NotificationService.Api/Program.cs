// Programa de arranque del Notification Service.
// Resumen del flujo (alto nivel):
// Paso 0: Configuración de servicios (Options Jwt/Mongo, Infrastructure, Application, WebApi).
// Paso 1: El cliente llama al endpoint POST /api/notifications/email con JWT e Idempotency-Key.
// Paso 2: Controller valida el DTO y pasa al UseCase.
// Paso 3: UseCase aplica idempotencia (Mongo), sanitiza, renderiza y encola el email.
// Paso 4: Se responde 202 Accepted (queued) con correlationId.
// Paso 5: Un BackgroundService consume la cola y envía el correo (proveedor primario/fallback),
//         actualizando el log en Mongo y el estado de idempotencia.

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

app.Run();

public partial class Program { } // For integration tests