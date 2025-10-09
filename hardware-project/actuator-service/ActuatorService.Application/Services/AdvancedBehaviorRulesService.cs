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
/// </summary>
public class AdvancedBehaviorRulesService
{
    private readonly IActuatorRepository _actuatorRepository;
    private readonly IControlOutputRepository _controlOutputRepository;
    private readonly IRoutineValidationService _routineValidationService;
    private readonly IRoutineExecutionService _routineExecutionService;
    private readonly IActuatorStateMachine _stateMachine;
    private readonly IPinBlockManager _pinBlockManager;
    private readonly AdvancedRulesConfiguration _config;
    private readonly ILogger<AdvancedBehaviorRulesService> _logger;

    private DateTime? _lastRecirculationAt;

    public AdvancedBehaviorRulesService(
        IActuatorRepository actuatorRepository,
        IControlOutputRepository controlOutputRepository,
        IRoutineValidationService routineValidationService,
        IRoutineExecutionService routineExecutionService,
        IActuatorStateMachine stateMachine,
        IPinBlockManager pinBlockManager,
        IOptions<AdvancedRulesConfiguration> config,
        ILogger<AdvancedBehaviorRulesService> logger)
    {
        _actuatorRepository = actuatorRepository;
        _controlOutputRepository = controlOutputRepository;
        _routineValidationService = routineValidationService;
        _routineExecutionService = routineExecutionService;
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
            // Get control outputs for recirculation pumps
            var allOutputs = await _controlOutputRepository.GetAllAsync();
            var waterPumpOutput = allOutputs.FirstOrDefault(o => o.Name == "Duración de Riego");
            var airPumpOutput = allOutputs.FirstOrDefault(o => o.Name == "Duración de Aireación");

            if (waterPumpOutput == null || airPumpOutput == null)
            {
                _logger.LogWarning("⚠️ Cannot execute recirculation - pump outputs not found");
                return;
            }

            // Create recirculation routine
            var recirculationDuration = _config.WaterHeater.RecirculationDurationSeconds;
            var steps = new List<RoutineStepDto>
            {
                new() { OutputVariable = airPumpOutput.Id, Power = "ON", Duration = 30 },
                new() { OutputVariable = waterPumpOutput.Id, Power = "ON", Duration = recirculationDuration },
                new() { OutputVariable = airPumpOutput.Id, Power = "ON", Duration = 30 }
            };

            var resolvedSteps = await _routineValidationService.ValidateAndResolveStepsAsync(steps);
            var esp32Ids = resolvedSteps.Select(s => s.Esp32Id).Distinct().ToList();

            if (esp32Ids.Count == 0)
            {
                _logger.LogWarning("⚠️ No valid steps for recirculation");
                return;
            }

            var resolvedRoutine = new ResolvedRoutineDto
            {
                RoutineId = "auto_recirculation",
                Esp32Id = esp32Ids.First(),
                ResolvedSteps = resolvedSteps
            };

            await _routineExecutionService.ScheduleRoutinesAsync(
                new List<ResolvedRoutineDto> { resolvedRoutine },
                esp32Ids.First());

            _lastRecirculationAt = now;

            _logger.LogInformation("✅ Automatic recirculation scheduled successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to execute automatic recirculation");
        }
    }
}
