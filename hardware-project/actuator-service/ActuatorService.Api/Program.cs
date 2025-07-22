using ActuatorService.Infrastructure;
using HydroEspinaca.Shared.Extensions;
using HydroEspinaca.Shared.Mongo;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

builder.Services.AddMongoSettings(builder.Configuration);
builder.Services.AddSingleton<MongoDbContext>();
builder.Services.AddInfrastructure();

app.Run();

