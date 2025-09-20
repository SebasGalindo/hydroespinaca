using BffService.Api.Extensions;
using BffService.Api.Middleware;
using BffService.Application;
using BffService.Domain;
using BffService.Infrastructure;
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
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplicationServices();
builder.Services.AddDomainServices();
builder.Services.AddBffServiceApi(builder.Configuration);

// ✅ CRITICAL: Register the shared and specific exception mappers
builder.Services.AddSingleton<HydroEspinaca.Shared.Errors.ProblemDetailsFactory>();
builder.Services.AddSingleton<IExceptionToProblemDetailsMapper, BffService.Api.Services.BffServiceExceptionMapper>();

var app = builder.Build();

// Configure middleware pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "BFF Service API v1")
    );
}

app.UseMiddleware<BffService.Api.Middleware.GlobalExceptionMiddleware>();
app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health").AllowAnonymous();

app.Run();

public partial class Program { } // For integration tests