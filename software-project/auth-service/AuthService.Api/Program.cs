using AuthService.Api.Middleware;
using AuthService.Application;
using AuthService.Infrastructure;
using AuthService.Infrastructure.Services;
using AuthService.Web;
using HydroEspinaca.Shared.Options;

var builder = WebApplication.CreateBuilder(args);

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

var app = builder.Build();

// Middleware
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


    // //Run data seeding
    //using (var scope = app.Services.CreateScope())
    //{
    //  var seedingService = scope.ServiceProvider.GetRequiredService<DataSeedingService>();
    //  await seedingService.SeedInitialDataAsync();
    //}

}
app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health").AllowAnonymous();

app.Run();

// Make Program class accessible for integration tests
//public partial class Program { }