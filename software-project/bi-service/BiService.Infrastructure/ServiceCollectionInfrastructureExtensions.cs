using BiService.Domain.Entities;
using BiService.Domain.Interfaces;
using BiService.Infrastructure.Persistence.Mappings;
using BiService.Infrastructure.Persistence.Models;
using BiService.Infrastructure.Persistence.Repositories;
using HydroEspinaca.Shared.Extensions;
using HydroEspinaca.Shared.Mongo.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BiService.Infrastructure;

/// <summary>
/// Extensiones de <see cref="IServiceCollection"/> para registrar los servicios de la capa de infraestructura del módulo BI.
/// </summary>
public static class ServiceCollectionInfrastructureExtensions
{
    /// <summary>
    /// Registra la configuración de MongoDB, los mappers de entidades y los repositorios.
    /// </summary>
    /// <param name="services">Colección de servicios del contenedor de inyección de dependencias.</param>
    /// <param name="configuration">Configuración de la aplicación.</param>
    /// <returns>La colección de servicios para encadenamiento fluido.</returns>
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMongoSettings(configuration);

        // Mappers
        services.AddScoped<IEntityMapper<CostConfigVersion, CostConfigVersionDocument>, CostConfigVersionMapper>();
        services.AddScoped<IEntityMapper<ManualConsumptionEntry, ManualConsumptionEntryDocument>, ManualConsumptionEntryMapper>();
        services.AddScoped<IEntityMapper<ProductionRecord, ProductionRecordDocument>, ProductionRecordMapper>();

        // Repositories
        services.AddScoped<ICostConfigVersionRepository, CostConfigVersionRepository>();
        services.AddScoped<IManualConsumptionEntryRepository, ManualConsumptionEntryRepository>();
        services.AddScoped<IProductionRecordRepository, ProductionRecordRepository>();

        return services;
    }
}
