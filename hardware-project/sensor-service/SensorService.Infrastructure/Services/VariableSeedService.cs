using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using SensorService.Domain.Interfaces;
using SensorService.Infrastructure.Persistence.Models;
using HydroEspinaca.Shared.Mongo;
using HydroEspinaca.Shared.Enums;

namespace SensorService.Infrastructure.Services;

public class VariableSeedService : IVariableSeedService
{
    private readonly ILogger<VariableSeedService> _logger;
    private readonly IMongoCollection<VariableDocument> _collection;

    public VariableSeedService(
        ILogger<VariableSeedService> logger,
        MongoDbContext context)
    {
        _logger = logger;
        _collection = context.Database.GetCollection<VariableDocument>("variables");
    }

    public async Task<int> SeedDefaultVariablesAsync()
    {
        _logger.LogInformation("🌱 Starting variable seeding process");

        var defaultVariables = GetDefaultVariables();
        var seededCount = 0;

        foreach (var variable in defaultVariables)
        {
            // Check if variable with this code already exists
            var existingVariable = await _collection
                .Find(v => v.Code == variable.Code)
                .FirstOrDefaultAsync();

            if (existingVariable != null)
            {
                _logger.LogDebug("⏭️  Variable '{Code}' already exists, skipping", variable.Code);
                continue;
            }

            // Insert the variable
            await _collection.InsertOneAsync(variable);
            seededCount++;
            _logger.LogInformation("✅ Seeded variable: {Code} - {Name}", variable.Code, variable.Name);
        }

        if (seededCount > 0)
        {
            _logger.LogInformation("🎉 Seeding completed: {Count} variables added", seededCount);
        }
        else
        {
            _logger.LogInformation("ℹ️  No new variables to seed");
        }

        return seededCount;
    }

    private List<VariableDocument> GetDefaultVariables()
    {
        return new List<VariableDocument>
        {
            new VariableDocument
            {
                Code = "EC",
                Name = "Conductividad eléctrica",
                Unit = "mS/cm",
                Description = "Ion concentration in the nutrient solution",
                Type = VariableTypes.Analog,
                PhysicalMin = 0,
                PhysicalMax = 10,
                OptimalMin = 1.8,
                OptimalMax = 2.3,
                RegulationType = HydroEspinaca.Shared.Enums.RegulationType.Manual,
                LastModified = DateTime.UtcNow
            },
            new VariableDocument
            {
                Code = "T_AMB",
                Name = "Temp ambiente",
                Unit = "°C",
                Description = "Ambient temperature",
                Type = VariableTypes.Analog,
                PhysicalMin = -10,
                PhysicalMax = 40,
                OptimalMin = 18,
                OptimalMax = 22,
                RegulationType = HydroEspinaca.Shared.Enums.RegulationType.Automatic,
                LastModified = DateTime.UtcNow
            },
            new VariableDocument
            {
                Code = "HUM",
                Name = "Humedad",
                Unit = "%",
                Description = "Relative humidity of the environment",
                Type = VariableTypes.Analog,
                PhysicalMin = 0,
                PhysicalMax = 100,
                OptimalMin = 60,
                OptimalMax = 75,
                RegulationType = HydroEspinaca.Shared.Enums.RegulationType.Automatic,
                LastModified = DateTime.UtcNow
            },
            new VariableDocument
            {
                Code = "T_WAT",
                Name = "Temp del Agua",
                Unit = "°C",
                Description = "Water temperature",
                Type = VariableTypes.Analog,
                PhysicalMin = -10,
                PhysicalMax = 85,
                OptimalMin = 18,
                OptimalMax = 23,
                RegulationType = HydroEspinaca.Shared.Enums.RegulationType.Automatic,
                LastModified = DateTime.UtcNow
            },
            new VariableDocument
            {
                Code = "WL",
                Name = "Nivel de agua",
                Unit = "cm",
                Description = "Nivel de agua en el tanque medido por sensor ultrasónico",
                Type = VariableTypes.Analog,
                PhysicalMin = 2,
                PhysicalMax = 400,
                OptimalMin = 3,
                OptimalMax = 8,
                RegulationType = HydroEspinaca.Shared.Enums.RegulationType.Manual,
                LastModified = DateTime.UtcNow
            },
            new VariableDocument
            {
                Code = "LUMINOSITY",
                Name = "Luminosidad",
                Unit = "lux",
                Description = "Light intensity measured by BH1750 sensor",
                Type = VariableTypes.Analog,
                PhysicalMin = 0,
                PhysicalMax = 65535,
                OptimalMin = 10000,
                OptimalMax = 12000,
                RegulationType = HydroEspinaca.Shared.Enums.RegulationType.Automatic,
                LastModified = DateTime.UtcNow
            },
            new VariableDocument
            {
                Code = "PH",
                Name = "Nivel de pH",
                Unit = "pH",
                Description = "Acidity or alkalinity of the nutrient solution",
                Type = VariableTypes.Analog,
                PhysicalMin = 0,
                PhysicalMax = 14,
                OptimalMin = 5.5,
                OptimalMax = 6.5,
                RegulationType = HydroEspinaca.Shared.Enums.RegulationType.Manual,
                LastModified = DateTime.UtcNow
            }
        };
    }
}
