using ActuatorService.Domain.Entities;
using ActuatorService.Domain.Interfaces;
using ActuatorService.Infrastructure.Persistence.Mappers;
using ActuatorService.Infrastructure.Persistence.Mappings;
using ActuatorService.Infrastructure.Persistence.Models;
using ActuatorService.Infrastructure.Persistence.Repositories;
using HydroEspinaca.Shared.Extensions;
using HydroEspinaca.Shared.Mongo.Interfaces;
using HydroEspinaca.Shared.Mqtt;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ActuatorService.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddMongoSettings(configuration)
            .AddMqttSettings(configuration)
            .AddApiKeySettings(configuration);

        // Mappers
        services.AddScoped<IEntityMapper<ActuatorCommand, ActuatorCommandDocument>, CommandMapper>();
        services.AddScoped<IEntityMapper<Actuator, ActuatorDocument>, ActuatorMapper>();

        // Repositories
        services.AddScoped<ICommandLogRepository, MongoCommandLogRepository>();
        services.AddScoped<IActuatorRepository, MongoActuatorRepository>();

        // MQTT Publisher
        services.AddScoped<ICommandPublisher, MqttCommandPublisher>();
        services.AddSingleton<IMqttClientService, MqttClientService>();

        return services;
    }
}
