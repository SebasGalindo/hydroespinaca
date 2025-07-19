using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SensorService.Application.UseCases;

namespace SensorService.Infrastructure.Services;

public class AggregateWorker : BackgroundService
{
    private readonly ILogger<AggregateWorker> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(10);

    public AggregateWorker(
        ILogger<AggregateWorker> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("🚀 AggregateWorker started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ExecuteAggregationCycleAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error during aggregation job");
            }

            await Task.Delay(_interval, stoppingToken);
        }

        _logger.LogInformation("🛑 AggregateWorker stopped");
    }

    private async Task ExecuteAggregationCycleAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<ProcessAggregatesUseCase>();

        var result = await useCase.ExecuteAsync(DateTime.UtcNow);

        _logger.LogDebug(
            "Aggregation cycle completed: {ProcessedCount} processed, {SkippedCount} skipped, {DeletedCount} deleted",
            result.ProcessedCount, result.SkippedCount, result.DeletedReadingsCount);
    }
}