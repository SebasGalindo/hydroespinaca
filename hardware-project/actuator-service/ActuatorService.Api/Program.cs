using ActuatorService.Api.Configurations;
using ActuatorService.Api.Middleware;
using ActuatorService.Application;
using ActuatorService.Application.Validators.Actuators;
using ActuatorService.Infrastructure;
using FluentValidation;
using HydroEspinaca.Shared.Extensions;
using SensorService.Api.Middleware;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------
// ⚙️ CONFIGURACIÓN
// ---------------------------
builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

// ---------------------------
// 🔌 SHARED OPTIONS
builder.Services
    .AddMongoSettings(builder.Configuration)
    .AddMqttSettings(builder.Configuration)
    .AddApiKeySettings(builder.Configuration);

builder.Services.AddValidatorsFromAssemblyContaining<CreateActuatorValidator>();

// ---------------------------
// 🧱 DEPENDENCIAS DE CAPAS
// ---------------------------
builder.Services.AddInfrastructure();
builder.Services.AddApplication();

// ---------------------------
// 🌐 CONTROLLERS + SWAGGER
// ---------------------------
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerWithApiKey("ActuatorService", "v1");

// ---------------------------
// 🧪 HEALTH CHECKS
// ---------------------------
builder.Services.AddHealthChecks();

// ---------------------------
// 🔊 LOGGING
// ---------------------------

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

if (builder.Environment.IsDevelopment())
{
    builder.Logging.SetMinimumLevel(LogLevel.Debug);
}
else
{
    builder.Logging.SetMinimumLevel(LogLevel.Warning);
}

// ---------------------------
// 🏁 APP PIPELINE
// ---------------------------
var app = builder.Build();



if (app.Environment.IsDevelopment() || app.Environment.IsStaging())
{
    app.UseSwaggerDocs("ActuatorService");
}

if (!app.Environment.IsDevelopment())
    app.UseHttpsRedirection();

app.MapGet("/", context =>
{
    context.Response.Redirect("/docs", permanent: false);
    return Task.CompletedTask;
});


app.UseMiddleware<ApiKeyMiddleware>();
app.UseMiddleware<GlobalExceptionMiddleware>();

app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapControllers();


app.Run();
