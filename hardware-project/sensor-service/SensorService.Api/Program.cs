using FluentValidation;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.OpenApi.Models;
using SensorService.Api.Middleware;
using SensorService.Application.Interfaces;
using SensorService.Application.Interfaces.UseCases.ProcessReadingBatch;
using SensorService.Application.Services;
using SensorService.Application.UseCases.ProcessReadingBatch;
using SensorService.Application.Validators.Esp32Node;
using SensorService.Domain.Config;
using SensorService.Domain.Interfaces;
using SensorService.Domain.Services;
using SensorService.Infrastructure.Persistence.Repositories;
using SensorService.Infrastructure.Services;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------
// 📦 CONFIGURACIÓN & BINDINGS
// ---------------------------

builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

// Mongo
builder.Services
    .AddOptions<MongoSettings>()
    .Bind(builder.Configuration.GetSection("Mongo"))
    .ValidateDataAnnotations()
    .ValidateOnStart();

// MQTT
builder.Services
    .AddOptions<MqttSettings>()
    .Bind(builder.Configuration.GetSection("Mqtt"));

// API Key
builder.Services
    .AddOptions<ApiKeySettings>()
    .Bind(builder.Configuration.GetSection("ApiKey"))
    .ValidateDataAnnotations()
    .ValidateOnStart();


// ---------------------------
// 🔌 INFRA: Repositorios
// ---------------------------
builder.Services.AddScoped<ISensorRepository, MongoSensorRepository>();
builder.Services.AddScoped<IReadingRepository, MongoReadingRepository>();
builder.Services.AddScoped<ISensorAlertRepository, MongoSensorAlertRepository>();
builder.Services.AddScoped<IVariableRepository, MongoVariableRepository>();
builder.Services.AddScoped<IAggregateRepository, MongoAggregateRepository>();
builder.Services.AddScoped<IEsp32NodeRepository, MongoEsp32NodeRepository>();

// ---------------------------
// 💼 APLICACIÓN: Servicios
// ---------------------------
builder.Services.AddScoped<ISensorService, SensorApplicationService>();
builder.Services.AddScoped<IReadingService, ReadingService>();
builder.Services.AddScoped<ISensorAlertService, SensorAlertService>();
builder.Services.AddScoped<IAggregateService, AggregateService>();
builder.Services.AddScoped<IVariableService, VariableService>();

// ---------------------------
// 🧠 DOMINIO: Servicios
// ---------------------------
builder.Services.AddScoped<IAlertCalculationService, AlertCalculationService>();

// ---------------------------
// 🔄 BACKGROUND WORKERS
// ---------------------------
builder.Services.AddHostedService<AggregateWorker>();
builder.Services.AddHostedService<Esp32OfflineWorker>();
builder.Services.AddHostedService<MqttClientService>();

// ---------------------------
// 🧪 USE CASES
// ---------------------------
builder.Services.AddScoped<IProcessReadingBatchUseCase, ProcessReadingBatchUseCase>();
builder.Services.AddScoped<IResolveOfflineAlertsUseCase, ResolveOfflineAlertsUseCase>();
builder.Services.AddScoped<IMatchReadingsWithSensorsUseCase, MatchReadingsWithSensorsUseCase>();
builder.Services.AddScoped<IGenerateAlertsUseCase, GenerateAlertsUseCase>();
builder.Services.AddScoped<IGenerateInactiveSensorAlertsUseCase, GenerateInactiveSensorAlertsUseCase>();

// ---------------------------
// ✅ VALIDADORES (FluentValidation)
// ---------------------------
builder.Services.AddValidatorsFromAssemblyContaining<Esp32NodeCreateValidator>();

// ---------------------------
// 🩺 HEALTH CHECKS
// ---------------------------
builder.Services.AddHealthChecks();

// ---------------------------
// 🌐 CONTROLLERS + JSON OPTIONS
// ---------------------------
builder.Services.AddControllers()
    .AddJsonOptions(opts =>
    {
        opts.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

// ---------------------------
// 📄 SWAGGER + API KEY AUTH
// ---------------------------
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "SensorService API", Version = "v1" });

    c.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
    {
        Description = "API Key necesaria para acceder a los endpoints. Usa el header: X-API-Key",
        In = ParameterLocation.Header,
        Name = "X-API-Key",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "ApiKeyScheme"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "ApiKey"
                },
                In = ParameterLocation.Header
            },
            new List<string>()
        }
    });
});

// ---------------------------
// 🔊 LOGGING
// ---------------------------
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Debug);

// ---------------------------
// 🏁 APP PIPELINE
// ---------------------------
var app = builder.Build();
if (app.Environment.IsDevelopment() || app.Environment.IsStaging())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Sensor Service API");
        c.RoutePrefix = "docs";
    });
}
app.UseHttpsRedirection();

app.MapGet("/", context =>
{
    context.Response.Redirect("/docs", permanent: false);
    return Task.CompletedTask;
});

// Middlewares personalizados
app.UseMiddleware<ApiKeyMiddleware>();
app.UseMiddleware<GlobalExceptionMiddleware>();

// Seguridad (futuro JWT)
app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapControllers();
app.Run();