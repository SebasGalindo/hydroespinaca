using HydroEspinaca.Shared.Mongo;
using HydroEspinaca.Shared.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace HydroEspinaca.Shared.Extensions;

public static class MongoExtensions
{
    public static IServiceCollection AddMongoSettings(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<MongoSettings>()
            .Bind(configuration.GetSection("Mongo"))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<MongoDbContext>();
        services.AddSingleton<IMongoDatabase>(sp =>
        {
            var context = sp.GetRequiredService<MongoDbContext>();
            return context.Database;
        });

        return services;
    }
}
