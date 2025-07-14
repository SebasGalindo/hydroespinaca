using FluentValidation;
using Microsoft.OpenApi.Models;
using SensorService.Api.Middleware;
using SensorService.Application.Interfaces;
using SensorService.Application.Services;
using SensorService.Application.Validators;
using SensorService.Domain.Config;
using SensorService.Domain.Interfaces;
using SensorService.Infrastructure.Mqtt;
using SensorService.Infrastructure.Persistence.Repositories;
using SensorService.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// ✅ Configuración
builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

// ✅ Bind y validación de configuración
builder.Services
    .AddOptions<MongoSettings>()
    .Bind(builder.Configuration.GetSection("Mongo"))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services
    .AddOptions<MqttSettings>()
    .Bind(builder.Configuration.GetSection("Mqtt"));

// ✅ Servicios
// Repositorios (Infraestructura)
builder.Services.AddSingleton<ISensorRepository, MongoSensorRepository>();
builder.Services.AddSingleton<IReadingRepository, MongoReadingRepository>();
builder.Services.AddSingleton<ISensorAlertRepository, MongoSensorAlertRepository>();
builder.Services.AddSingleton<IVariableRepository, MongoVariableRepository>();
builder.Services.AddSingleton<IAggregateRepository, MongoAggregateRepository>();

// Servicios de aplicación
builder.Services.AddScoped<ISensorService, SensorApplicationService>();
builder.Services.AddScoped<IReadingService, ReadingService>();
builder.Services.AddScoped<ISensorAlertService, SensorAlertService>();
builder.Services.AddScoped<IAggregateService, AggregateService>();
builder.Services.AddScoped<IVariableService, VariableService>();


builder.Services.AddHostedService<AggregateWorker>();
builder.Services.AddHostedService<Esp32OfflineWorker>();

builder.Services.AddHostedService<MqttClientService>();
builder.Services
    .AddOptions<ApiKeySettings>()
    .Bind(builder.Configuration.GetSection("ApiKey"))
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddValidatorsFromAssemblyContaining<SensorCreateValidator>();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// ✅ Swagger con autenticación por API Key
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

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Debug);



var app = builder.Build();

// ✅ Dev tools
if (app.Environment.IsDevelopment())
    app.UseDeveloperExceptionPage();

// ✅ Swagger siempre
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Sensor Service API");
    c.RoutePrefix = "docs";
});

app.UseHttpsRedirection();

app.MapGet("/", context =>
{
    context.Response.Redirect("/docs", permanent: false);
    return Task.CompletedTask;
});

Console.WriteLine($"Running in: {app.Environment.EnvironmentName}");

app.UseMiddleware<ApiKeyMiddleware>();
app.UseMiddleware<GlobalExceptionMiddleware>();

// Si en futuro agregás JWT:
// app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.Run();
