using ActuatorService.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ActuatorService.Application.Services;

/// <summary>
/// Background service that monitors actuators for advanced behavior rule violations
/// </summary>
public class AdvancedBehaviorMonitoringService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AdvancedBehaviorMonitoringService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(1);

    public AdvancedBehaviorMonitoringService(
        IServiceProvider serviceProvider,
        ILogger<AdvancedBehaviorMonitoringService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("🤖 Advanced Behavior Monitoring Service started");

        // Wait on startup
        await Task.Delay(TimeSpan.FromSeconds(45), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckAdvancedRulesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error in advanced behavior monitoring cycle");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }

        _logger.LogInformation("🛑 Advanced Behavior Monitoring Service stopped");
    }

    private async Task CheckAdvancedRulesAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var behaviorService = scope.ServiceProvider.GetRequiredService<AdvancedBehaviorRulesService>();
        var pinBlockManager = scope.ServiceProvider.GetRequiredService<IPinBlockManager>();

        // Monitor max time violations
        await behaviorService.MonitorMaxTimeViolationsAsync();

        // Cleanup expired cooldowns
        await pinBlockManager.CleanupExpiredCooldownsAsync();
    }
}
