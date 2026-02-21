using WeatherService.Api.Middleware;
using WeatherService.Api.Services;
using WeatherService.Application;
using WeatherService.Domain.Settings;
using WeatherService.Infrastructure;
using WeatherService.Web;
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

// Configure settings
builder.Services.Configure<OpenWeatherSettings>(
    builder.Configuration.GetSection("ExternalApis:OpenWeather")
);
builder.Services.Configure<WeatherSettings>(
    builder.Configuration.GetSection("Weather")
);

// Register layers
builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddApplicationServices();
builder.Services.AddWebApi(builder.Configuration, builder.Environment);

// Register exception mapper
builder.Services.AddSingleton<ProblemDetailsFactory>();
builder.Services.AddSingleton<IExceptionToProblemDetailsMapper, WeatherServiceExceptionMapper>();

var app = builder.Build();

// Configure middleware
if (app.Environment.IsDevelopment())
{
    builder.Host.UseDefaultServiceProvider(options =>
    {
        options.ValidateScopes = true;
        options.ValidateOnBuild = true;
    });

    app.UseSwagger();
    app.UseSwaggerUI(c =>
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Weather Service API v1")
    );
}

app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health").AllowAnonymous();

await app.RunAsync();

// Make Program class accessible for integration tests
public partial class Program { }
