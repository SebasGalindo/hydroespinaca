using HydroEspinaca.Shared.Extensions;
using HydroEspinaca.Shared.Mongo.Interfaces;
using HydroEspinaca.Shared.Mqtt;
using HydroEspinaca.Shared.Authentication.Services;
using HydroEspinaca.Shared.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SensorService.Domain.Entities;
using SensorService.Domain.Interfaces;
using SensorService.Infrastructure.Persistence.Mappers;
using SensorService.Infrastructure.Persistence.Models;
using SensorService.Infrastructure.Persistence.Repositories;
using SensorService.Infrastructure.Services;

namespace SensorService.Infrastructure;

public static class ServiceCollectionInfrastructureExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddMongoSettings(configuration)
            .AddMqttSettings(configuration);

        // Repositorios
        services.AddScoped<ISensorRepository, MongoSensorRepository>();
        services.AddScoped<IReadingRepository, MongoReadingRepository>();
        services.AddScoped<ISensorAlertRepository, MongoSensorAlertRepository>();
        services.AddScoped<IVariableRepository, MongoVariableRepository>();
        services.AddScoped<IAggregateRepository, MongoAggregateRepository>();
        services.AddScoped<IEsp32NodeRepository, MongoEsp32NodeRepository>();
        services.AddScoped<IEsp32AlertRepository, MongoEsp32AlertRepository>();

        // Mappers
        services.AddScoped<IEntityMapper<Sensor, SensorDocument>, SensorMapper>();
        services.AddScoped<IEntityMapper<Reading, ReadingDocument>, ReadingMapper>();
        services.AddScoped<IEntityMapper<SensorAlert, SensorAlertDocument>, SensorAlertMapper>();
        services.AddScoped<IEntityMapper<Variable, VariableDocument>, VariableMapper>();
        services.AddScoped<IEntityMapper<Esp32Node, Esp32NodeDocument>, Esp32NodeMapper>();
        services.AddScoped<IEntityMapper<Aggregate, AggregateDocument>, AggregateMapper>();
        services.AddScoped<IEntityMapper<Esp32Alert, Esp32AlertDocument>, Esp32AlertMapper>();

        // M2M Authentication configuration
        services.Configure<M2MAuthOptions>(configuration.GetSection("M2M"));
        services.AddTransient<M2MTokenService>();

        // Services
        services.AddSingleton<IMqttClientService, MqttClientService>();
        services.AddScoped<IVariableMigrationService, VariableMigrationService>();
        services.AddHttpClient<ICriticalAlertNotificationService, CriticalAlertNotificationService>();
     
        return services;
    }
}
