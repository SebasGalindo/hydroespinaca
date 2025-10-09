using ActuatorService.Domain.Entities;
using ActuatorService.Domain.Interfaces;
using HydroEspinaca.Shared.Enums;
using Microsoft.Extensions.Logging;

namespace ActuatorService.Infrastructure.Services;

public class DataSeedingService
{
    private readonly IActuatorRepository _actuatorRepository;
    private readonly IControlOutputRepository _controlOutputRepository;
    private readonly IInternalRoutineRepository _internalRoutineRepository;
    private readonly ILogger<DataSeedingService> _logger;

    public DataSeedingService(
        IActuatorRepository actuatorRepository,
        IControlOutputRepository controlOutputRepository,
        IInternalRoutineRepository internalRoutineRepository,
        ILogger<DataSeedingService> logger)
    {
        _actuatorRepository = actuatorRepository;
        _controlOutputRepository = controlOutputRepository;
        _internalRoutineRepository = internalRoutineRepository;
        _logger = logger;
    }

    public async Task SeedInitialDataAsync()
    {
        try
        {
            _logger.LogInformation("Starting data seeding for ActuatorService...");
            await SeedActuatorsAsync();
            await SeedControlOutputsAsync();
            await SeedInternalRoutinesAsync();
            _logger.LogInformation("Data seeding completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during data seeding");
            throw;
        }
    }

private async Task SeedActuatorsAsync()
{
    var actuators = new[]
    {
        new
        {
            PhysicalId = "FAN-001",
            Code = "Ventiladores",
            Esp32Id = "6883fff7b079309f3ba4f238",
            Pin = "27",
            Mode = ActuatorMode.PWM,
            Type = ActuatorType.Fan,
            Location = "invernadero"
        },
        new
        {
            PhysicalId = "HEATER-001",
            Code = "termoventilador",
            Esp32Id = "6883fff7b079309f3ba4f238",
            Pin = "17",
            Mode = ActuatorMode.DIGITAL,
            Type = ActuatorType.Heater,
            Location = "invernadero"
        },
        new
        {
            PhysicalId = "LED-001",
            Code = "luz-amplio-espectro",
            Esp32Id = "6883fff7b079309f3ba4f238",
            Pin = "18",
            Mode = ActuatorMode.DIGITAL,
            Type = ActuatorType.Led,
            Location = "invernadero"
        },
        new
        {
            PhysicalId = "AIR-001",
            Code = "piedra-difusora",
            Esp32Id = "6883fff7b079309f3ba4f238",
            Pin = "5",
            Mode = ActuatorMode.DIGITAL,
            Type = ActuatorType.AirPump,
            Location = "invernadero"
        },
        new
        {
            PhysicalId = "PUMP-001",
            Code = "bomba-agua",
            Esp32Id = "6883fff7b079309f3ba4f238",
            Pin = "19",
            Mode = ActuatorMode.DIGITAL,
            Type = ActuatorType.Pump,
            Location = "invernadero"
        },
        new
        {
            PhysicalId = "WATER_HEATER-001",
            Code = "calefactor-agua",
            Esp32Id = "6883fff7b079309f3ba4f238",
            Pin = "25",
            Mode = ActuatorMode.DIGITAL,
            Type = ActuatorType.Heater,
            Location = "invernadero"
        }
    };

    var createdCount = 0;
    var totalActuators = actuators.Length;

    foreach (var actuatorData in actuators)
    {
        // Check if actuator exists by getting all and filtering by PhysicalId
        var allActuators = await _actuatorRepository.GetAllAsync();
        var existing = allActuators.FirstOrDefault(a => a.PhysicalId == actuatorData.PhysicalId);
        if (existing == null)
        {
            var actuator = new Actuator
            {
                PhysicalId = actuatorData.PhysicalId,
                Code = actuatorData.Code,
                Esp32Id = actuatorData.Esp32Id,
                Pin = actuatorData.Pin,
                Mode = actuatorData.Mode,
                Type = actuatorData.Type,
                Location = actuatorData.Location
            };

            await _actuatorRepository.AddAsync(actuator);
            createdCount++;
            _logger.LogInformation("Created actuator: {PhysicalId}", actuatorData.PhysicalId);
        }
    }

    _logger.LogInformation("🛠️ Actuators: Created {CreatedCount} new actuators, {ExistingCount} already existed. Total: {Total}",
        createdCount, totalActuators - createdCount, totalActuators);
}

    private async Task SeedControlOutputsAsync()
    {
        // Primero obtener los actuadores por PhysicalId para obtener sus IDs
        var allActuators = await _actuatorRepository.GetAllAsync();
        var fanActuator = allActuators.FirstOrDefault(a => a.PhysicalId == "FAN-001");
        var heaterActuator = allActuators.FirstOrDefault(a => a.PhysicalId == "HEATER-001");
        var ledActuator = allActuators.FirstOrDefault(a => a.PhysicalId == "LED-001");
        var airPumpActuator = allActuators.FirstOrDefault(a => a.PhysicalId == "AIR-001");
        var waterPumpActuator = allActuators.FirstOrDefault(a => a.PhysicalId == "PUMP-001");
        var waterHeaterActuator = allActuators.FirstOrDefault(a => a.PhysicalId == "WATER_HEATER-001");

        if (fanActuator == null || heaterActuator == null || ledActuator == null ||
            airPumpActuator == null || waterPumpActuator == null || waterHeaterActuator == null)
        {
            _logger.LogWarning("Some actuators not found. Skipping control outputs seeding.");
            return;
        }

        var controlOutputs = new[]
        {
            // Ventilador (FAN-001) - 2 variables
            new
            {
                Name = "Potencia del Ventilador",
                Description = "Control de potencia PWM del ventilador principal",
                Unit = "porcentaje",
                ActuatorId = fanActuator.Id,
                MinValue = 0.0,
                MaxValue = 100.0
            },
            new
            {
                Name = "Duración de Ventilación",
                Description = "Tiempo de operación del ventilador",
                Unit = "segundos",
                ActuatorId = fanActuator.Id,
                MinValue = 0.0,
                MaxValue = 3600.0
            },
            // Calefactor de Aire (HEATER-001)
            new
            {
                Name = "Duración de Calefacción de Aire",
                Description = "Tiempo de operación del calefactor de aire",
                Unit = "segundos",
                ActuatorId = heaterActuator.Id,
                MinValue = 0.0,
                MaxValue = 3600.0
            },
            // Luz (LED-001)
            new
            {
                Name = "Duración de Luz",
                Description = "Tiempo de iluminación",
                Unit = "segundos",
                ActuatorId = ledActuator.Id,
                MinValue = 0.0,
                MaxValue = 3600.0
            },
            // Bomba de Aire (AIR-001)
            new
            {
                Name = "Duración de Aireación",
                Description = "Tiempo de operación de la bomba de aire",
                Unit = "segundos",
                ActuatorId = airPumpActuator.Id,
                MinValue = 0.0,
                MaxValue = 3600.0
            },
            // Bomba de Agua (PUMP-001)
            new
            {
                Name = "Duración de Riego",
                Description = "Tiempo de operación de la bomba de agua",
                Unit = "segundos",
                ActuatorId = waterPumpActuator.Id,
                MinValue = 0.0,
                MaxValue = 3600.0
            },
            // Calefactor de Agua (WATER_HEATER-001)
            new
            {
                Name = "Duración de Calefacción de Agua",
                Description = "Tiempo de operación del calefactor de agua",
                Unit = "segundos",
                ActuatorId = waterHeaterActuator.Id,
                MinValue = 0.0,
                MaxValue = 3600.0
            }
        };

        var createdCount = 0;
        var totalOutputs = controlOutputs.Length;

        foreach (var outputData in controlOutputs)
        {
            // Check if control output already exists for this actuator and name
            var existingOutputs = await _controlOutputRepository.GetByActuatorIdAsync(outputData.ActuatorId);
            var existing = existingOutputs.FirstOrDefault(co => co.Name == outputData.Name);

            if (existing == null)
            {
                var controlOutput = new ControlOutput
                {
                    Name = outputData.Name,
                    Description = outputData.Description,
                    Unit = outputData.Unit,
                    ActuatorId = outputData.ActuatorId,
                    MinValue = outputData.MinValue,
                    MaxValue = outputData.MaxValue,
                    LastModified = DateTime.UtcNow
                };

                await _controlOutputRepository.AddAsync(controlOutput);
                createdCount++;
                _logger.LogInformation("Created control output: {Name} for actuator {ActuatorId}",
                    outputData.Name, outputData.ActuatorId);
            }
        }

        _logger.LogInformation("🎛️  Control Outputs: Created {CreatedCount} new outputs, {ExistingCount} already existed. Total: {Total}",
            createdCount, totalOutputs - createdCount, totalOutputs);
    }

    private async Task SeedInternalRoutinesAsync()
    {
        var esp32Id = "6883fff7b079309f3ba4f238"; // Same ESP32 as actuators

        // Get control outputs for reference
        var allControlOutputs = await _controlOutputRepository.GetAllAsync();
        var airPumpOutput = allControlOutputs.FirstOrDefault(co => co.Name == "Duración de Aireación");
        var waterPumpOutput = allControlOutputs.FirstOrDefault(co => co.Name == "Duración de Riego");

        if (airPumpOutput == null || waterPumpOutput == null)
        {
            _logger.LogWarning("⚠️  Cannot seed internal routines - required control outputs not found");
            return;
        }

        var routines = new[]
        {
            // Recirculation routine - every 4 hours
            new InternalRoutine
            {
                Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(),
                Name = "Recirculation",
                Description = "Routine to circulate nutrient solution every 4 hours",
                Esp32Id = esp32Id,
                Interval = TimeSpan.FromHours(4),
                StartTime = TimeSpan.Zero, // Start at midnight
                IsActive = true,
                Steps = new List<InternalRoutineStep>
                {
                    // Air pump for 60 seconds
                    new InternalRoutineStep
                    {
                        OutputVariable = airPumpOutput.Id,
                        Power = "ON",
                        Duration = 60,
                        Mode = "DIGITAL"
                    },
                    // Water pump for 8 minutes (480 seconds)
                    new InternalRoutineStep
                    {
                        OutputVariable = waterPumpOutput.Id,
                        Power = "ON",
                        Duration = 480,
                        Mode = "DIGITAL"
                    },
                    // Air pump for another 60 seconds
                    new InternalRoutineStep
                    {
                        OutputVariable = airPumpOutput.Id,
                        Power = "ON",
                        Duration = 60,
                        Mode = "DIGITAL"
                    }
                },
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            // Aeration routine - every 30 minutes
            new InternalRoutine
            {
                Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(),
                Name = "Aeration",
                Description = "Short routine for air circulation every 30 minutes",
                Esp32Id = esp32Id,
                Interval = TimeSpan.FromMinutes(30),
                StartTime = TimeSpan.Zero, // Start at midnight
                IsActive = true,
                Steps = new List<InternalRoutineStep>
                {
                    // Air pump for 5 minutes (300 seconds)
                    new InternalRoutineStep
                    {
                        OutputVariable = airPumpOutput.Id,
                        Power = "ON",
                        Duration = 300,
                        Mode = "DIGITAL"
                    }
                },
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        var createdCount = 0;
        var totalRoutines = routines.Length;

        foreach (var routine in routines)
        {
            // Check if routine already exists by name
            var existingRoutines = await _internalRoutineRepository.GetActiveRoutinesAsync();
            var existing = existingRoutines.FirstOrDefault(r => r.Name == routine.Name);

            if (existing == null)
            {
                await _internalRoutineRepository.AddAsync(routine);
                createdCount++;
                _logger.LogInformation("⏰ Created internal routine: {Name} (Interval: {Interval})",
                    routine.Name, routine.Interval);
            }
        }

        _logger.LogInformation("⏰ Internal Routines: Created {CreatedCount} new routines, {ExistingCount} already existed. Total: {Total}",
            createdCount, totalRoutines - createdCount, totalRoutines);
    }
}
