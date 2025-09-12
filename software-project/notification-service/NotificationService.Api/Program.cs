// Programa de arranque del Notification Service.
// Resumen del flujo (alto nivel):
// Paso 0: Configuraci�n de servicios (Options Jwt/Mongo, Infrastructure, Application, WebApi).
// Paso 1: El cliente llama al endpoint POST /api/notifications/email con JWT e Idempotency-Key.
// Paso 2: Controller valida el DTO y pasa al UseCase.
// Paso 3: UseCase aplica idempotencia (Mongo), sanitiza, renderiza y encola el email.
// Paso 4: Se responde 202 Accepted (queued) con correlationId.
// Paso 5: Un BackgroundService consume la cola y env�a el correo (proveedor primario/fallback),
//         actualizando el log en Mongo y el estado de idempotencia.

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using HydroEspinaca.Shared.Options;
using NotificationService.Web;
using NotificationService.Infrastructure;
using NotificationService.Application;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using NotificationService.Api.HealthChecks;
using NotificationService.Api.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Configura settings tipados (JWT y Mongo) desde appsettings.*
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));
builder.Services.Configure<MongoSettings>(builder.Configuration.GetSection("Mongo"));

// Registra las capas siguiendo el patr�n del auth-service
builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddApplicationServices();
builder.Services.AddWebApi(builder.Configuration);
builder.Services.AddHealthChecks()
    .AddCheck<EmailProvidersHealthCheck>("email_providers");

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    builder.Host.UseDefaultServiceProvider(options =>
    {
        options.ValidateScopes = true;
        options.ValidateOnBuild = true;
    });

    // Swagger para explorar la API
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Notification Service API v1"));
}

// Pipeline b�sico: HTTPS, AuthN/Z, Controllers y Health

// Activar AuthN/Z solo si fue configurado en DI (Jwt presente) sin resolver scoped desde root
var isServiceChecker = app.Services.GetService<Microsoft.Extensions.DependencyInjection.IServiceProviderIsService>();
var hasAuth = isServiceChecker?.IsService(typeof(Microsoft.AspNetCore.Authentication.IAuthenticationService)) ?? false;
if (hasAuth)
{
    app.UseAuthentication();
    app.UseAuthorization();
}
app.MapControllers();
// Middleware global de excepciones al final antes de Run
app.UseMiddleware<GlobalExceptionMiddleware>();
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
