using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using SensorService.Domain.Interfaces;
using SensorService.Infrastructure.Persistence.Models;
using HydroEspinaca.Shared.Mongo;
using HydroEspinaca.Shared.Enums;

namespace SensorService.Infrastructure.Services;

public class SensorSeedService : ISensorSeedService
{
    private readonly ILogger<SensorSeedService> _logger;
    private readonly IMongoCollection<SensorDocument> _collection;
    private readonly IEsp32NodeRepository _esp32Repository;

    public SensorSeedService(
        ILogger<SensorSeedService> logger,
        MongoDbContext context,
        IEsp32NodeRepository esp32Repository)
    {
        _logger = logger;
        _collection = context.Database.GetCollection<SensorDocument>("sensors");
        _esp32Repository = esp32Repository;
    }

    public async Task<int> SeedDefaultSensorsAsync()
    {
        _logger.LogInformation("🌱 Starting sensor seeding process");

        // Check if there are any ESP32 nodes available
        var esp32Nodes = await _esp32Repository.GetAllAsync();
        if (!esp32Nodes.Any())
        {
            _logger.LogWarning("⚠️ No ESP32 nodes found. Please seed ESP32 nodes first before seeding sensors.");
            return 0;
        }

        // Use the first available ESP32 node for seed data
        var defaultEsp32Id = esp32Nodes.First().Id;
        _logger.LogInformation("📟 Using ESP32 node: {Esp32Id} for seed sensors", defaultEsp32Id);

        var defaultSensors = GetDefaultSensors(defaultEsp32Id);
        var seededCount = 0;

        foreach (var sensor in defaultSensors)
        {
            // Check if sensor with this code already exists
            var existingSensor = await _collection
                .Find(s => s.Code == sensor.Code)
                .FirstOrDefaultAsync();

            if (existingSensor != null)
            {
                _logger.LogDebug("⏭️  Sensor '{Code}' already exists, skipping", sensor.Code);
                continue;
            }

            // Insert the sensor
            await _collection.InsertOneAsync(sensor);
            seededCount++;
            _logger.LogInformation("✅ Seeded sensor: {Code} - {PhysicalId} with variables [{Variables}]",
                sensor.Code, sensor.PhysicalId, string.Join(", ", sensor.Variables));
        }

        if (seededCount > 0)
        {
            _logger.LogInformation("🎉 Seeding completed: {Count} sensors added", seededCount);
        }
        else
        {
            _logger.LogInformation("ℹ️  No new sensors to seed");
        }

        return seededCount;
    }

    private List<SensorDocument> GetDefaultSensors(string esp32Id)
    {
        return new List<SensorDocument>
        {
            new SensorDocument
            {
                Code = "bh1750-01",
                PhysicalId = "BH1750-A1",
                Location = "nutrient-tank",
                Esp32Id = esp32Id,
                SamplingFrequency = 60,
                Variables = new List<string> { "LUMINOSITY" },
                Status = SensorStatus.Active,
                CreatedAt = DateTime.UtcNow,
                AllowMissing = true
            },
            new SensorDocument
            {
                Code = "dht22-01",
                PhysicalId = "DHT22-A1",
                Location = "greenhouse",
                Esp32Id = esp32Id,
                SamplingFrequency = 60,
                Variables = new List<string> { "T_AMB", "HUM" },
                Status = SensorStatus.Active,
                CreatedAt = DateTime.UtcNow,
                AllowMissing = false
            },
            new SensorDocument
            {
                Code = "tds-01",
                PhysicalId = "TDS-A1",
                Location = "nutrient-tank",
                Esp32Id = esp32Id,
                SamplingFrequency = 60,
                Variables = new List<string> { "EC" },
                Status = SensorStatus.Active,
                CreatedAt = DateTime.UtcNow,
                AllowMissing = false
            },
            new SensorDocument
            {
                Code = "sen0161-01",
                PhysicalId = "SEN0161-A1",
                Location = "nutrient-tank",
                Esp32Id = esp32Id,
                SamplingFrequency = 60,
                Variables = new List<string> { "PH" },
                Status = SensorStatus.Active,
                CreatedAt = DateTime.UtcNow,
                AllowMissing = false
            },
            new SensorDocument
            {
                Code = "ntc-01",
                PhysicalId = "NTC-A1",
                Location = "nutrient-tank",
                Esp32Id = esp32Id,
                SamplingFrequency = 60,
                Variables = new List<string> { "T_WAT" },
                Status = SensorStatus.Active,
                CreatedAt = DateTime.UtcNow,
                AllowMissing = false
            },
            new SensorDocument
            {
                Code = "ultrasonido-01",
                PhysicalId = "HC-SR04-A1",
                Location = "nutrient-tank",
                Esp32Id = esp32Id,
                SamplingFrequency = 60,
                Variables = new List<string> { "WL" },
                Status = SensorStatus.Active,
                CreatedAt = DateTime.UtcNow,
                AllowMissing = false
            }
        };
    }
}
