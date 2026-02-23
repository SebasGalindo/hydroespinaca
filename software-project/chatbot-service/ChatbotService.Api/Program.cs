using ChatbotService.Application;
using ChatbotService.Infrastructure;
using ChatbotService.Api.Extensions;
using ChatbotService.Api.Middleware;
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
builder.Services.AddApplication();
builder.Services.AddChatbotServiceApi(builder.Configuration);

// ✅ CRITICAL: Register the shared and specific exception mappers
builder.Services.AddSingleton<ProblemDetailsFactory>();

var app = builder.Build();

// Configure middleware pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Chatbot Service API v1")
    );
}

app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health").AllowAnonymous();

// Inicializar knowledge base de manuales al arrancar
using (var scope = app.Services.CreateScope())
{
    var initializer = scope.ServiceProvider.GetService<ChatbotService.Infrastructure.Services.KnowledgeBaseInitializer>();
    if (initializer != null)
    {
        await initializer.InitializeAsync(CancellationToken.None);
    }
}

await app.RunAsync();

public partial class Program { } // For integration tests
