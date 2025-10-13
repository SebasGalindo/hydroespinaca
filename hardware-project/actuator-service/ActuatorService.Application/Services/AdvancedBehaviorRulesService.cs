using ActuatorService.Application.Configuration;
using ActuatorService.Application.DTOs;
using ActuatorService.Application.Interfaces;
using ActuatorService.Domain.Interfaces;
using HydroEspinaca.Shared.DTOs.Actuator;
using HydroEspinaca.Shared.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ActuatorService.Application.Services;

/// <summary>
/// Implements advanced behavior rules for critical actuators
/// Refactored to work with direct actuator codes instead of ControlOutputs
/// </summary>
public class AdvancedBehaviorRulesService
{
    private readonly IActuatorRepository _actuatorRepository;
    private readonly IActuatorCodeResolver _actuatorCodeResolver;
    private readonly ICommandExecutionService _commandExecutionService;
    private readonly IActuatorStateMachine _stateMachine;
    private readonly IPinBlockManager _pinBlockManager;
    private readonly AdvancedRulesConfiguration _config;
    private readonly ILogger<AdvancedBehaviorRulesService> _logger;

    private DateTime? _lastRecirculationAt;

    public AdvancedBehaviorRulesService(
        IActuatorRepository actuatorRepository,
        IActuatorCodeResolver actuatorCodeResolver,
        ICommandExecutionService commandExecutionService,
        IActuatorStateMachine stateMachine,
        IPinBlockManager pinBlockManager,
        IOptions<AdvancedRulesConfiguration> config,
        ILogger<AdvancedBehaviorRulesService> logger)
    {
        _actuatorRepository = actuatorRepository;
        _actuatorCodeResolver = actuatorCodeResolver;
        _commandExecutionService = commandExecutionService;
        _stateMachine = stateMachine;
        _pinBlockManager = pinBlockManager;
        _config = config.Value;
        _logger = logger;
    }

    /// <summary>
    /// Checks if a command should be allowed based on advanced rules
    /// </summary>
    public async Task<(bool Allowed, string? BlockReason)> ValidateCommandAsync(
        string actuatorId,
        PowerState targetState)
    {
        var actuator = await _actuatorRepository.GetByIdAsync(actuatorId);
        if (actuator == null)
            return (true, null);

        // Check cooldown
        if (await _pinBlockManager.IsBlockedAsync(actuatorId))
        {
            var cooldownInfo = await _pinBlockManager.GetCooldownInfoAsync(actuatorId);
            var reason = $"Actuator in cooldown: {cooldownInfo?.Reason}. Expires in {cooldownInfo?.RemainingTime:hh\\:mm\\:ss}";
            _logger.LogWarning("🚫 Command blocked: {Reason}", reason);
            return (false, reason);
        }

        // Check lighting time restrictions
        if (actuator.Code == "Luz de Espectro Completo" && targetState == PowerState.ON)
        {
            var localTime = DateTime.UtcNow.AddHours(-5); // Colombia UTC-5
            var currentHour = localTime.Hour;

            if (currentHour < _config.FullSpectrumLight.AllowedStartHour ||
                currentHour >= _config.FullSpectrumLight.AllowedEndHour)
            {
                var reason = $"Bloqueo horario: fuera del rango permitido ({_config.FullSpectrumLight.AllowedStartHour}:00 - {_config.FullSpectrumLight.AllowedEndHour}:00)";
                _logger.LogWarning("🚫 {ActuatorCode}: {Reason}", actuator.Code, reason);
                return (false, reason);
            }
        }

        return (true, null);
    }

    /// <summary>
    /// Executes post-command behaviors (e.g., recirculation after heater OFF)
    /// </summary>
    public async Task ExecutePostCommandBehaviorsAsync(
        string actuatorId,
        PowerState newState)
    {
        var actuator = await _actuatorRepository.GetByIdAsync(actuatorId);
        if (actuator == null)
            return;

        // Water heater OFF → trigger recirculation
        if (actuator.Code == "Calefactor de Agua" && newState == PowerState.OFF)
        {
            await HandleWaterHeaterOffAsync(actuator);
        }
    }

    /// <summary>
    /// Monitors actuators for max time violations
    /// </summary>
    public async Task MonitorMaxTimeViolationsAsync()
    {
        var now = DateTime.UtcNow;
        var allActuators = await _actuatorRepository.GetAllAsync();

        foreach (var actuator in allActuators)
        {
            var state = _stateMachine.GetState(actuator.Id);
            if (state == null || state.State != PowerState.ON)
                continue;

            var onDuration = now - state.LastUpdated;

            // Humidifier: Max continuous time
            if (actuator.PhysicalId == "HUMIDIFIER-001")
            {
                var maxTime = TimeSpan.FromMinutes(_config.Humidifier.MaxContinuousOnMinutes);
                if (onDuration > maxTime)
                {
                    _logger.LogWarning("⚠️ Humidifier exceeded max time: {Duration:hh\\:mm\\:ss} > {MaxTime:hh\\:mm\\:ss}",
                        onDuration, maxTime);

                    // Turn OFF
                    _stateMachine.UpdateState(actuator.Id, PowerState.OFF, commandId: "max_time_violation");

                    // Apply cooldown
                    var cooldownDuration = TimeSpan.FromMinutes(_config.Humidifier.CooldownMinutes);
                    await _pinBlockManager.BlockForAsync(
                        actuator.Id,
                        cooldownDuration,
                        "Auto-off: tiempo máximo alcanzado");

                    _logger.LogInformation("🔒 Humidifier blocked for {Duration:hh\\:mm\\:ss}",
                        cooldownDuration);
                }
            }

            // Full Spectrum Light: Time restrictions
            if (actuator.Code == "Luz de Espectro Completo")
            {
                var localTime = now.AddHours(-5); // Colombia UTC-5
                var currentHour = localTime.Hour;

                // Auto-off after 18:00
                if (currentHour >= _config.FullSpectrumLight.AllowedEndHour)
                {
                    _logger.LogWarning("⚠️ Full spectrum light ON after allowed hours. Auto-off triggered.");

                    _stateMachine.UpdateState(actuator.Id, PowerState.OFF, commandId: "time_restriction");

                    var cooldownDuration = TimeSpan.FromMinutes(_config.FullSpectrumLight.CooldownMinutes);
                    await _pinBlockManager.BlockForAsync(
                        actuator.Id,
                        cooldownDuration,
                        "Auto-off: fuera de horario permitido");
                }

                // Max continuous time
                var maxTime = TimeSpan.FromHours(_config.FullSpectrumLight.MaxContinuousOnHours);
                if (onDuration > maxTime)
                {
                    _logger.LogWarning("⚠️ Full spectrum light exceeded max time: {Duration:hh\\:mm\\:ss}",
                        onDuration);

                    _stateMachine.UpdateState(actuator.Id, PowerState.OFF, commandId: "max_time_violation");

                    var cooldownDuration = TimeSpan.FromMinutes(_config.FullSpectrumLight.CooldownMinutes);
                    await _pinBlockManager.BlockForAsync(
                        actuator.Id,
                        cooldownDuration,
                        "Auto-off: tiempo máximo continuo excedido");
                }
            }
        }
    }

    private async Task HandleWaterHeaterOffAsync(Domain.Entities.Actuator heater)
    {
        // Check if recirculation was done recently
        var now = DateTime.UtcNow;
        var minInterval = TimeSpan.FromMinutes(_config.WaterHeater.MinIntervalBetweenRecirculationsMinutes);

        if (_lastRecirculationAt.HasValue && (now - _lastRecirculationAt.Value) < minInterval)
        {
            _logger.LogInformation("⏭️ Skipping recirculation - executed recently ({LastExecution})",
                _lastRecirculationAt.Value);
            return;
        }

        _logger.LogInformation("🔄 Water heater OFF detected - triggering automatic recirculation");

        try
        {
            // Resolve actuators by code
            var waterPumpActuator = await _actuatorCodeResolver.ResolveAsync("BombaRiego");
            var airPumpActuator = await _actuatorCodeResolver.ResolveAsync("BombaAire");

            if (waterPumpActuator == null || airPumpActuator == null)
            {
                _logger.LogWarning("⚠️ Cannot execute recirculation - pump actuators not found (BombaRiego or BombaAire)");
                return;
            }

            // Create individual commands for recirculation sequence
            var recirculationDuration = _config.WaterHeater.RecirculationDurationSeconds;
            var commands = new List<ActuatorControlDto>
            {
                // Pre-aeration: 30 seconds
                new() { ActuatorCode = "BombaAire", Power = "ON", Duration = 30 },
                // Main irrigation: configured duration
                new() { ActuatorCode = "BombaRiego", Power = "ON", Duration = recirculationDuration },
                // Post-aeration: 30 seconds
                new() { ActuatorCode = "BombaAire", Power = "ON", Duration = 30 }
            };

            // Resolve commands to physical actuators
            var resolvedCommands = new List<ResolvedCommandDto>();
            foreach (var command in commands)
            {
                var actuator = await _actuatorCodeResolver.ResolveAsync(command.ActuatorCode);
                if (actuator == null)
                {
                    _logger.LogWarning("⚠️ Actuator {ActuatorCode} not found, skipping command", command.ActuatorCode);
                    continue;
                }

                resolvedCommands.Add(new ResolvedCommandDto
                {
                    ActuatorCode = command.ActuatorCode,
                    ActuatorId = actuator.Id,
                    Esp32Id = actuator.Esp32Id,
                    Pin = actuator.Pin,
                    Mode = actuator.Mode,
                    Power = command.Power,
                    DutyCycle = command.DutyCycle,
                    Duration = command.Duration
                });
            }

            if (resolvedCommands.Count == 0)
            {
                _logger.LogWarning("⚠️ No valid commands for recirculation");
                return;
            }

            // Ensure all commands belong to same ESP32
            var esp32Id = resolvedCommands.First().Esp32Id;
            if (resolvedCommands.Any(c => c.Esp32Id != esp32Id))
            {
                _logger.LogWarning("⚠️ Recirculation commands span multiple ESP32 devices - cannot execute");
                return;
            }

            // Schedule individual commands
            await _commandExecutionService.ScheduleCommandsAsync(resolvedCommands, esp32Id);

            _lastRecirculationAt = now;

            _logger.LogInformation("✅ Automatic recirculation scheduled successfully ({CommandCount} commands)",
                resolvedCommands.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to execute automatic recirculation");
        }
    }
}
