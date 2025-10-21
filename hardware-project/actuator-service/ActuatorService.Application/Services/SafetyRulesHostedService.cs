using ActuatorService.Application.Configuration;
using ActuatorService.Application.DTOs;
using ActuatorService.Application.Interfaces;
using ActuatorService.Domain.Interfaces;
using HydroEspinaca.Shared.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ActuatorService.Application.Services;

/// <summary>
/// Background service that monitors actuators and applies safety rules.
/// Automatically turns off actuators that exceed their maximum allowed ON time.
/// Sends physical shutdown commands via MQTT when violations are detected.
/// </summary>
public class SafetyRulesHostedService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SafetyRulesHostedService> _logger;
    private readonly SafetyRulesConfiguration _config;
    private readonly IServiceScopeFactory _scopeFactory;

    public SafetyRulesHostedService(
        IServiceProvider serviceProvider,
        IOptions<SafetyRulesConfiguration> config,
        ILogger<SafetyRulesHostedService> logger,
        IServiceScopeFactory scopeFactory)
    {
        _serviceProvider = serviceProvider;
        _config = config.Value;
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("🛡️ Safety Rules Service started");

        if (_config.Limits == null || !_config.Limits.Any())
        {
            _logger.LogWarning("⚠️ No safety limits configured. Safety monitoring disabled.");
            return;
        }

        _logger.LogInformation("📋 Configured safety limits: {Count}", _config.Limits.Count);
        foreach (var limit in _config.Limits)
        {
            _logger.LogInformation("  - {Description}: {MaxTime}s", limit.Description, limit.MaxOnTimeSeconds);
        }

        // Wait a bit on startup to ensure other services are ready
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

        var checkInterval = TimeSpan.FromSeconds(_config.CheckIntervalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckSafetyRulesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error in safety rules check cycle");
            }

            await Task.Delay(checkInterval, stoppingToken);
        }

        _logger.LogInformation("🛑 Safety Rules Service stopped");
    }

    private async Task CheckSafetyRulesAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var actuatorRepository = scope.ServiceProvider.GetRequiredService<IActuatorRepository>();
        var stateMachine = scope.ServiceProvider.GetRequiredService<IActuatorStateMachine>();
        var actuatorService = scope.ServiceProvider.GetRequiredService<IActuatorService>();

        var now = DateTime.UtcNow;

        _logger.LogDebug("🔍 Checking safety rules at {Time}", now);

        // Get all actuators
        var allActuators = await actuatorRepository.GetAllAsync();

        foreach (var actuator in allActuators)
        {
            // Find applicable safety limit
            var limit = _config.Limits.FirstOrDefault(l =>
                (!string.IsNullOrEmpty(l.PhysicalId) && l.PhysicalId == actuator.PhysicalId) ||
                (!string.IsNullOrEmpty(l.Code) && l.Code == actuator.Code));

            if (limit == null)
                continue;

            // Get current state from state machine
            var state = stateMachine.GetState(actuator.Id);

            if (state == null || state.State != PowerState.ON)
                continue;

            // Check if actuator has been ON for too long (using LastUpdated from state)
            var onDuration = now - state.LastUpdated;
            var maxAllowed = TimeSpan.FromSeconds(limit.MaxOnTimeSeconds);

            if (onDuration > maxAllowed)
            {
                _logger.LogWarning("⚠️ SAFETY VIOLATION: {ActuatorCode} ({PhysicalId}) has been ON for {Duration:hh\\:mm\\:ss}, exceeding limit of {MaxTime:hh\\:mm\\:ss}",
                    actuator.Code, actuator.PhysicalId, onDuration, maxAllowed);

                // Update state machine to OFF
                stateMachine.UpdateState(actuator.Id, PowerState.OFF, commandId: "safety_shutdown");

                // Send physical shutdown command to ESP32
                await SendPhysicalShutdownAsync(actuator);

                _logger.LogWarning("🛡️ Safety shutdown executed for {ActuatorCode} ({PhysicalId}) - State updated and physical OFF command sent",
                    actuator.Code, actuator.PhysicalId);
            }
        }
    }

    private async Task SendPhysicalShutdownAsync(Domain.Entities.Actuator actuator)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var commandExecutionService = scope.ServiceProvider.GetRequiredService<ICommandExecutionService>();
            var actuatorCodeResolver = scope.ServiceProvider.GetRequiredService<IActuatorCodeResolver>();

            // Resolve actuator to get latest configuration
            var resolvedActuator = await actuatorCodeResolver.ResolveAsync(actuator.Code);

            if (resolvedActuator == null)
            {
                _logger.LogWarning("⚠️ Cannot send physical shutdown for {ActuatorCode} - actuator not found",
                    actuator.Code);
                return;
            }

            // Create OFF command
            var shutdownCommand = new DTOs.ResolvedCommandDto
            {
                ActuatorCode = resolvedActuator.Code,
                ActuatorId = resolvedActuator.Id,
                Esp32Id = resolvedActuator.Esp32Id,
                Pin = resolvedActuator.Pin,
                Mode = resolvedActuator.Mode,
                Power = HydroEspinaca.Shared.Constants.ActuatorConstants.PowerStates.Off,
                DutyCycle = null,
                Duration = 0
            };

            // Send immediate reset command (bypasses pin locking for safety)
            await commandExecutionService.SendImmediateResetCommandsAsync(
                new List<DTOs.ResolvedCommandDto> { shutdownCommand },
                resolvedActuator.Esp32Id);

            _logger.LogInformation("✅ Physical shutdown command sent to ESP32 {Esp32Id} for {ActuatorCode}",
                resolvedActuator.Esp32Id, actuator.Code);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to send physical shutdown command for {ActuatorCode}", actuator.Code);
        }
    }
}
