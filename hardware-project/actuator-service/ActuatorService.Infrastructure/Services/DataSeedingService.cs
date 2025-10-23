using ActuatorService.Domain.Entities;
using ActuatorService.Domain.Interfaces;
using HydroEspinaca.Shared.Enums;
using Microsoft.Extensions.Logging;

namespace ActuatorService.Infrastructure.Services;

public class DataSeedingService
{
    private readonly IActuatorRepository _actuatorRepository;
    private readonly IInternalRoutineRepository _internalRoutineRepository;
    private readonly ILogger<DataSeedingService> _logger;

    public DataSeedingService(
        IActuatorRepository actuatorRepository,
        IInternalRoutineRepository internalRoutineRepository,
        ILogger<DataSeedingService> logger)
    {
        _actuatorRepository = actuatorRepository;
        _internalRoutineRepository = internalRoutineRepository;
        _logger = logger;
    }

    public async Task SeedInitialDataAsync()
    {
        try
        {
            _logger.LogInformation("Starting data seeding for ActuatorService...");
            await SeedActuatorsAsync();
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
                Type = ActuatorType.Pump,
                Location = "invernadero"
            },
            new
            {
                PhysicalId = "WATER-001",
                Code = "bomba-agua",
                Esp32Id = "6883fff7b079309f3ba4f238",
                Pin = "19",
                Mode = ActuatorMode.DIGITAL,
                Type = ActuatorType.Pump,
                Location = "invernadero"
            },
            new
            {
                PhysicalId = "WATER-HEATER-001",
                Code = "calefactor-agua",
                Esp32Id = "6883fff7b079309f3ba4f238",
                Pin = "4",
                Mode = ActuatorMode.DIGITAL,
                Type = ActuatorType.Heater,
                Location = "invernadero"
            },
            new
            {
                PhysicalId = "HUMIDIFIER-001",
                Code = "humidificador-ultrasonico",
                Esp32Id = "6883fff7b079309f3ba4f238",
                Pin = "16",
                Mode = ActuatorMode.DIGITAL,
                Type = ActuatorType.Humidifier,
                Location = "invernadero"
            }
        };

        var createdCount = 0;
        var totalActuators = actuators.Length;

        foreach (var actuatorData in actuators)
        {
            // Check if actuator already exists (by PhysicalId for idempotency)
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
                    Location = actuatorData.Location,
                    Status = ActuatorStatus.Active
                };

                await _actuatorRepository.AddAsync(actuator);
                createdCount++;
                _logger.LogInformation("✅ Created actuator: {PhysicalId} - {Code}", actuatorData.PhysicalId, actuatorData.Code);
            }
        }

        _logger.LogInformation("🛠️ Actuators: Created {CreatedCount} new actuators, {ExistingCount} already existed. Total: {Total}",
            createdCount, totalActuators - createdCount, totalActuators);
    }

    private async Task SeedInternalRoutinesAsync()
    {
        var esp32Id = "6883fff7b079309f3ba4f238"; // Same ESP32 as actuators

        // Build Reinicio del sistema routine with steps for all actuators using ActuatorCode
        var systemResetSteps = await BuildSystemResetStepsAsync();

        var routines = new[]
        {
            // Recirculation routine - every 4 hours
            new InternalRoutine
            {
                Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(),
                Name = "Recirculación",
                Description = "Rutina para circular la solución nutritiva cada 2 horas",
                Esp32Id = esp32Id,
                Interval = TimeSpan.FromHours(2),
                IsActive = true,
                Steps = new List<InternalRoutineStep>
                {
                    // Air pump for 60 seconds
                    new InternalRoutineStep
                    {
                        OutputVariable = "piedra-difusora", // ActuatorCode
                        Power = "ON",
                        Duration = 60,
                        Mode = "DIGITAL"
                    },
                    // Water pump for 8 minutes (480 seconds)
                    new InternalRoutineStep
                    {
                        OutputVariable = "bomba-agua", // ActuatorCode
                        Power = "ON",
                        Duration = 480,
                        Mode = "DIGITAL"
                    },
                    // Air pump for another 60 seconds
                    new InternalRoutineStep
                    {
                        OutputVariable = "piedra-difusora", // ActuatorCode
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
                Name = "Aireación",
                Description = "Rutina corta para la circulación de aire cada 30 minutos",
                Esp32Id = esp32Id,
                Interval = TimeSpan.FromMinutes(30),
                IsActive = true,
                Steps = new List<InternalRoutineStep>
                {
                    // Air pump for 5 minutes (300 seconds)
                    new InternalRoutineStep
                    {
                        OutputVariable = "piedra-difusora", // ActuatorCode
                        Power = "ON",
                        Duration = 300,
                        Mode = "DIGITAL"
                    }
                },
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            // Reinicio del sistema routine - manual trigger via /commands/jobs/clear
            new InternalRoutine
            {
                Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(),
                Name = "Reinicio del sistema",
                Description = "Rutina del sistema para restablecer todos los actuadores al estado OFF (activada por /commands/jobs/clear)",
                Esp32Id = esp32Id,
                Interval = TimeSpan.FromDays(999), // Not time-triggered, manually invoked
                IsActive = false, // Not scheduled by timer, manually triggered
                Steps = systemResetSteps,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        var createdCount = 0;
        var totalRoutines = routines.Length;

        foreach (var routine in routines)
        {
            // Check if routine already exists by name (regardless of active status)
            var existing = await _internalRoutineRepository.GetByNameAsync(routine.Name);

            if (existing == null)
            {
                await _internalRoutineRepository.AddAsync(routine);
                createdCount++;
                _logger.LogInformation("⏰ Created internal routine: {Name} (Interval: {Interval})",
                    routine.Name, routine.Interval);
            }
            else
            {
                // Update existing routine steps to use ActuatorCode
                existing.Steps = routine.Steps;
                existing.UpdatedAt = DateTime.UtcNow;
                await _internalRoutineRepository.UpdateAsync(existing);
                _logger.LogInformation("⏰ Updated internal routine: {Name} to use ActuatorCode", routine.Name);
            }
        }

        _logger.LogInformation("⏰ Internal Routines: Created {CreatedCount} new routines, {ExistingCount} already existed/updated. Total: {Total}",
            createdCount, totalRoutines - createdCount, totalRoutines);
    }

    private async Task<List<InternalRoutineStep>> BuildSystemResetStepsAsync()
    {
        var allActuators = await _actuatorRepository.GetAllAsync();
        var resetSteps = new List<InternalRoutineStep>();

        foreach (var actuator in allActuators)
        {
            var step = new InternalRoutineStep
            {
                OutputVariable = actuator.Code, // Now using ActuatorCode directly
                Duration = 0.1, // Very short duration just to apply the reset command
                Mode = actuator.Mode.ToString()
            };

            // For PWM actuators, use DutyCycle = 0.0 (turns off)
            // For DIGITAL actuators, use Power = "OFF"
            if (actuator.Mode == ActuatorMode.PWM)
            {
                step.DutyCycle = 0.0;
                step.Power = null; // PWM doesn't use Power field
            }
            else
            {
                step.Power = "OFF";
                step.DutyCycle = null; // DIGITAL doesn't use DutyCycle
            }

            resetSteps.Add(step);
            _logger.LogDebug("🔧 Added reset step for {Code} (Pin: {Pin}, Mode: {Mode})",
                actuator.Code, actuator.Pin, actuator.Mode);
        }

        _logger.LogInformation("🔧 Built Reinicio del sistema routine with {StepCount} steps using ActuatorCode", resetSteps.Count);
        return resetSteps;
    }
}
