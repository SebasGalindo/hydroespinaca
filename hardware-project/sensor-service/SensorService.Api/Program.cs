using SensorService.Api.Extensions;
using SensorService.Api.Middleware;
using SensorService.Api.Services;
using SensorService.Application;
using SensorService.Domain;
using SensorService.Infrastructure;
using HydroEspinaca.Shared.Authentication.Interfaces;
using HydroEspinaca.Shared.Errors;

var builder = WebApplication.CreateBuilder(args);

// ✅ Configure Clean Architecture layers
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplicationServices();
builder.Services.AddDomainServices();
builder.Services.AddUseCases();
builder.Services.AddBackgroundWorkers();
builder.Services.AddValidation();
builder.Services.AddSensorServiceApi(builder.Configuration);

// ✅ CRITICAL: Register the specific exception mapper
builder.Services.AddSingleton<ProblemDetailsFactory>();
builder.Services.AddSingleton<IExceptionToProblemDetailsMapper, SensorServiceExceptionMapper>();

var app = builder.Build();

// Configure middleware based on environment
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Sensor Service API v1")
    );
}

if (app.Environment.IsProduction())
{
    app.UseHsts();
}

app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health").AllowAnonymous();

app.Run();

public partial class Program { } // For integration tests
