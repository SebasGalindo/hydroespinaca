using FluentValidation;
using HydroEspinaca.Shared.Extensions;
using HydroEspinaca.Shared.Options;
using Microsoft.OpenApi.Models;
using SensorService.Api.Middleware;
using SensorService.Application.Interfaces;
using SensorService.Application.Interfaces.UseCases.AggregateWorker;
using SensorService.Application.Interfaces.UseCases.Esp32OfflineWorker;
using SensorService.Application.Interfaces.UseCases.ProcessReadingBatch;
using SensorService.Application.Services;
using SensorService.Application.UseCases;
using SensorService.Application.UseCases.Esp32OfflineWorker;
using SensorService.Application.UseCases.ProcessReadingBatch;
using SensorService.Application.Validators.Esp32Node;
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
    .AddMongoSettings(builder.Configuration)
    .AddApiKeySettings(builder.Configuration)
    .AddMqttSettings(builder.Configuration);

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
builder.Services.AddScoped<IAggregationService, AggregationService>();
builder.Services.AddScoped<IEsp32StatusService, Esp32StatusService>();

// ---------------------------
// 🔄 BACKGROUND WORKERS
// ---------------------------
builder.Services.AddHostedService<AggregateWorker>();
builder.Services.AddHostedService<Esp32OfflineWorker>();
builder.Services.AddHostedService<MqttClientService>();

// ---------------------------
// 🧪 USE CASES
// ---------------------------
builder.Services.AddScoped<IResolveOfflineAlertsUseCase, ResolveOfflineAlertsUseCase>();
builder.Services.AddScoped<IMatchReadingsWithSensorsUseCase, MatchReadingsWithSensorsUseCase>();
builder.Services.AddScoped<IGenerateAlertsUseCase, GenerateAlertsUseCase>();
builder.Services.AddScoped<IGenerateInactiveSensorAlertsUseCase, GenerateInactiveSensorAlertsUseCase>();

builder.Services.AddScoped<IProcessReadingBatchUseCase, ProcessReadingBatchUseCase>();
builder.Services.AddScoped<IProcessAggregatesUseCase, ProcessAggregatesUseCase>();
builder.Services.AddScoped<ICheckEsp32OfflineStatusUseCase, CheckEsp32OfflineStatusUseCase>();


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

if (builder.Environment.IsDevelopment())

    builder.Logging.SetMinimumLevel(LogLevel.Debug);

else
    builder.Logging.SetMinimumLevel(LogLevel.Warning);

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

// Custom middlewares
app.UseMiddleware<ApiKeyMiddleware>();
app.UseMiddleware<GlobalExceptionMiddleware>();

// Security (future JWT)
app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapControllers();
app.Run();