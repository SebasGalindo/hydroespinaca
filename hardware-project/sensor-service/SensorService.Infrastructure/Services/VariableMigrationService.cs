using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using SensorService.Domain.Interfaces;
using SensorService.Infrastructure.Persistence.Models;
using HydroEspinaca.Shared.Mongo;

namespace SensorService.Infrastructure.Services;

public class VariableMigrationService : IVariableMigrationService
{
    private readonly ILogger<VariableMigrationService> _logger;
    private readonly MongoDbContext _context;
    private readonly IMongoCollection<VariableDocument> _collection;

    public VariableMigrationService(
        ILogger<VariableMigrationService> logger,
        MongoDbContext context)
    {
        _logger = logger;
        _context = context;
        _collection = _context.Database.GetCollection<VariableDocument>("variables");
    }

    public async Task<int> MigrateVariablesToNewSchemaAsync()
    {
        _logger.LogInformation("🔄 Iniciando migración de variables a nuevo esquema con rangos físicos y óptimos");

        // Buscar variables que todavía usan el esquema antiguo (tienen MinValue/MaxValue pero no PhysicalMin/PhysicalMax)
        var filter = Builders<VariableDocument>.Filter.And(
            Builders<VariableDocument>.Filter.Exists("minValue", true),
            Builders<VariableDocument>.Filter.Exists("physicalMin", false)
        );

        var oldVariables = await _collection.Find(filter).ToListAsync();
        
        if (oldVariables.Count == 0)
        {
            _logger.LogInformation("ℹ️ No se encontraron variables para migrar");
            return 0;
        }

        _logger.LogInformation("📊 Migrando {Count} variables al nuevo esquema", oldVariables.Count);

        var updates = new List<WriteModel<VariableDocument>>();

        foreach (var variable in oldVariables)
        {
            var (physicalMin, physicalMax, optimalMin, optimalMax) = GetRangesForVariable(variable.Name, variable.Unit);

            var updateFilter = Builders<VariableDocument>.Filter.Eq("_id", variable.Id);
            var updateBuilder = Builders<VariableDocument>.Update
                .Set(v => v.PhysicalMin, physicalMin)
                .Set(v => v.PhysicalMax, physicalMax)
                .Set(v => v.OptimalMin, optimalMin)
                .Unset("minValue")  // Remover campos antiguos
                .Unset("maxValue");

            // Set or unset OptimalMax based on whether it has a value
            if (optimalMax.HasValue)
            {
                updateBuilder = updateBuilder.Set(v => v.OptimalMax, optimalMax.Value);
            }
            else
            {
                updateBuilder = updateBuilder.Unset("optimalMax");
            }

            updates.Add(new UpdateOneModel<VariableDocument>(updateFilter, updateBuilder));

            _logger.LogDebug("🔧 Migrando variable '{Name}': Físico [{PhysicalMin}-{PhysicalMax}], Óptimo [{OptimalMin}-{OptimalMax}]", 
                variable.Name, physicalMin, physicalMax, optimalMin, optimalMax?.ToString() ?? "null");
        }

        // Ejecutar todas las actualizaciones en batch
        var bulkResult = await _collection.BulkWriteAsync(updates);

        _logger.LogInformation("✅ Migración completada: {ModifiedCount} variables actualizadas", bulkResult.ModifiedCount);

        return (int)bulkResult.ModifiedCount;
    }

    private (double physicalMin, double physicalMax, double optimalMin, double? optimalMax) GetRangesForVariable(string name, string unit)
    {
        // Definir rangos basados en el nombre y unidad de la variable
        var normalizedName = name.ToLowerInvariant();

        return normalizedName switch
        {
            var n when n.Contains("ph") => (0.0, 14.0, 5.5, 6.5),
            var n when n.Contains("temperature") || n.Contains("temperatura") => (-10.0, 50.0, 18.0, 24.0),
            var n when n.Contains("humidity") || n.Contains("humedad") => (0.0, 100.0, 60.0, 80.0),
            var n when n.Contains("conductivity") || n.Contains("conductividad") => (0.0, 3000.0, 1200.0, 1800.0),
            var n when n.Contains("dissolved oxygen") || n.Contains("oxigeno") => (0.0, 15.0, 6.0, 9.0),
            // Luminosity Clear - only minimum threshold
            var n when (n.Contains("light") || n.Contains("luz")) && (n.Contains("clear") || n.Contains("cantidad")) => (0.0, 100000.0, 3000.0, null),
            // Luminosity Index - only minimum threshold  
            var n when (n.Contains("light") || n.Contains("luz")) && (n.Contains("index") || n.Contains("indice")) => (0.0, 100.0, 65.0, null),
            // Generic light sensor - with range
            var n when n.Contains("light") || n.Contains("luz") => (0.0, 100000.0, 15000.0, 40000.0),
            var n when n.Contains("water level") || n.Contains("nivel agua") => (0.0, 100.0, 20.0, 80.0),
            var n when unit.ToLowerInvariant().Contains("ppm") => (0.0, 2000.0, 800.0, 1400.0),
            var n when unit.ToLowerInvariant().Contains("°c") => (-10.0, 50.0, 18.0, 24.0),
            var n when unit.ToLowerInvariant().Contains("%") => (0.0, 100.0, 40.0, 80.0),
            _ => (0.0, 1000.0, 200.0, 800.0) // Valores por defecto genéricos
        };
    }
}