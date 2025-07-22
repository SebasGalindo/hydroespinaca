using ActuatorService.Domain.Entities;
using ActuatorService.Domain.Interfaces;
using ActuatorService.Infrastructure.Persistence.Mappers;
using ActuatorService.Infrastructure.Persistence.Models;
using ActuatorService.Infrastructure.Persistence.Repositories;
using HydroEspinaca.Shared.Mongo.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace ActuatorService.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<IEntityMapper<ActuatorCommand, ActuatorCommandDocument>, CommandMapper>();
        services.AddScoped<ICommandLogRepository, MongoCommandLogRepository>();
        services.AddScoped<IActuatorRepository, MongoActuatorRepository>();
        return services;
    }
}
