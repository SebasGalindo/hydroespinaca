using AuthService.Application;
using AuthService.Application.Validators;
using AuthService.Infrastructure;
using AuthService.Web;
using HydroEspinaca.Shared.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));
builder.Services.Configure<MongoSettings>(builder.Configuration.GetSection("Mongo"));

// Register layers
builder.Services.AddInfrastructureServices(builder.Configuration); 
builder.Services.AddApplicationServices();    
builder.Services.AddWebApi();

var app = builder.Build();

// Middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Auth Service API v1"));
}

app.UseHttpsRedirection();
app.UseAuthorization();

app.MapControllers();

app.Run();
