using ActuatorService.Application.Interfaces;
using ActuatorService.Domain.Entities;
using ActuatorService.Domain.Interfaces;
using ActuatorService.Infrastructure.Http;
using ActuatorService.Infrastructure.Messaging.Mqtt;
using ActuatorService.Infrastructure.Persistence.Mappings;
using ActuatorService.Infrastructure.Persistence.Models;
using ActuatorService.Infrastructure.Persistence.Repositories;
using ActuatorService.Infrastructure.Services;
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
        services.AddScoped<IEntityMapper<Actuator, ActuatorDocument>, ActuatorMapper>();
        services.AddScoped<IEntityMapper<RoutineCommand, RoutineCommandDocument>, RoutineCommandMapper>();

        // Repositories
        services.AddScoped<IActuatorRepository, MongoActuatorRepository>();
        services.AddScoped<IRoutineCommandRepository, MongoRoutineCommandRepository>();

        // MQTT Publisher
        services.AddScoped<IRoutineCommandPublisher, MqttRoutineCommandPublisher>();
        services.AddSingleton<IMqttClientService, MqttClientService>();

        // MQTT Subscriber
        services.AddScoped<MqttRoutineCompletionSubscriber>();

        // Background Services
        services.AddHostedService<DatabaseCleanupService>();

        // Validation
        services.AddHttpClient<IEsp32ValidationService, Esp32ValidationService>(client =>
        {
            var sensorServiceUrl = configuration["SensorService:BaseUrl"];
            if (!string.IsNullOrEmpty(sensorServiceUrl))
            {
                client.BaseAddress = new Uri(sensorServiceUrl);
            }
        });


        return services;
    }
}
