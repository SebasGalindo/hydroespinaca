using AuthService.Api.Middleware;
using AuthService.Api.Services;
using AuthService.Application;
using AuthService.Infrastructure;
using AuthService.Infrastructure.Services;
using AuthService.Web;
using HydroEspinaca.Shared.Authentication.Interfaces;
using HydroEspinaca.Shared.Errors;
using HydroEspinaca.Shared.Options;

var builder = WebApplication.CreateBuilder(args);

// Configure logging
builder.Logging.ClearProviders();
builder.Logging.AddSimpleConsole(options =>
{
    options.IncludeScopes = false;
    options.SingleLine = true;
    options.TimestampFormat = "yyyy-MM-dd HH:mm:ss ";
});


// Configura settings y capas...
builder.Services.Configure<AuthService.Infrastructure.Security.JwtSettings>(
    builder.Configuration.GetSection("Jwt")
);
builder.Services.Configure<MongoSettings>(
    builder.Configuration.GetSection("Mongo")
);

builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddApplicationServices();
builder.Services.AddWebApi(builder.Configuration, builder.Environment);

// Register auth-service specific exception mapper
builder.Services.AddSingleton<ProblemDetailsFactory>();
builder.Services.AddSingleton<IExceptionToProblemDetailsMapper, AuthServiceExceptionMapper>();

var app = builder.Build();

// Configure middleware based on environment
if (app.Environment.IsDevelopment())
{
    builder.Host.UseDefaultServiceProvider(options =>
    {
        options.ValidateScopes = true;
        options.ValidateOnBuild = true;
    });

    app.UseSwagger();
    app.UseSwaggerUI(c =>
       c.SwaggerEndpoint("/swagger/v1/swagger.json", "Auth Service API v1")
    );

    // Run data seeding only in development
    using (var scope = app.Services.CreateScope())
    {
     var seedingService = scope.ServiceProvider.GetRequiredService<DataSeedingService>();
     await seedingService.SeedInitialDataAsync();
    }
}

// if (app.Environment.IsProduction())
// {
//     app.UseHsts();
// }

app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health").AllowAnonymous();

app.Run();

// Make Program class accessible for integration tests
public partial class Program { }