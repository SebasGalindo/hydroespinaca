using ActuatorService.Application.Configuration;
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
/// </summary>
public class SafetyRulesHostedService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SafetyRulesHostedService> _logger;
    private readonly SafetyRulesConfiguration _config;

    public SafetyRulesHostedService(
        IServiceProvider serviceProvider,
        IOptions<SafetyRulesConfiguration> config,
        ILogger<SafetyRulesHostedService> logger)
    {
        _serviceProvider = serviceProvider;
        _config = config.Value;
        _logger = logger;
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

                _logger.LogWarning("🛡️ Safety shutdown executed for {ActuatorCode} ({PhysicalId})",
                    actuator.Code, actuator.PhysicalId);

                // TODO: Consider publishing MQTT command to physically turn off the actuator
                // This requires accessing the MQTT publisher which is scoped
                // For now, we just update the state machine which will prevent new commands
            }
        }
    }
}
