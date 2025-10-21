using ActuatorService.Api.Extensions;
using ActuatorService.Api.Middleware;
using ActuatorService.Api.Services;
using ActuatorService.Application;
using ActuatorService.Infrastructure;
using ActuatorService.Infrastructure.Services;
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


// Service layers
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication();
builder.Services.AddActuatorServiceApi(builder.Configuration);

// Exception mapper
builder.Services.AddSingleton<ProblemDetailsFactory>();
builder.Services.AddSingleton<IExceptionToProblemDetailsMapper, ActuatorServiceExceptionMapper>();

var app = builder.Build();

// Configure middleware based on environment
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Actuator Service API v1"));
}

if (app.Environment.IsProduction())
{
    app.UseHsts();
}

app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health").AllowAnonymous();

// Seed initial data and synchronize actuator states
using (var scope = app.Services.CreateScope())
{
    var seedingService = scope.ServiceProvider.GetRequiredService<DataSeedingService>();
    await seedingService.SeedInitialDataAsync();

    // Synchronize actuator states on startup
    var startupSyncService = scope.ServiceProvider.GetRequiredService<ActuatorService.Application.Services.ActuatorStartupSyncService>();
    await startupSyncService.SynchronizeOnStartupAsync();
}

app.Run();

public partial class Program { }
