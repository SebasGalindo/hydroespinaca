using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SensorService.Domain.Entities;
using SensorService.Domain.Interfaces;

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
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();

                var sensorRepo = scope.ServiceProvider.GetRequiredService<ISensorRepository>();
                var readingRepo = scope.ServiceProvider.GetRequiredService<IReadingRepository>();
                var aggregateRepo = scope.ServiceProvider.GetRequiredService<IAggregateRepository>();

                await ProcessAggregatesAsync(sensorRepo, readingRepo, aggregateRepo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error during aggregation job");
            }

            await Task.Delay(_interval, stoppingToken);
        }
    }

    private async Task ProcessAggregatesAsync(
        ISensorRepository sensorRepo,
        IReadingRepository readingRepo,
        IAggregateRepository aggregateRepo)
    {
        var now = DateTime.UtcNow;
        var bucketTime = new DateTime(
            now.Year, now.Month, now.Day, now.Hour,
            (now.Minute / 10) * 10, 0, DateTimeKind.Utc
        );
        var windowStart = bucketTime.AddMinutes(-10);

        var sensors = await sensorRepo.GetAllAsync();

        foreach (var sensor in sensors)
        {
            foreach (var variableId in sensor.Variables)
            {
                var readings = await readingRepo.GetBySensorAndVariableAsync(sensor.Id, variableId, windowStart, bucketTime);
                if (readings.Count == 0) continue;

                var values = readings.Select(x => x.Value).ToList();

                var aggregate = new Aggregate
                {
                    Id = $"{sensor.Id}-{variableId}-{bucketTime:yyyyMMddHHmm}",
                    SensorId = sensor.Id,
                    VariableId = variableId,
                    Avg = values.Average(),
                    Min = values.Min(),
                    Max = values.Max(),
                    Count = values.Count,
                    Timestamp = bucketTime
                };

                var existing = await aggregateRepo.GetBySensorAndVariableAndTimestampAsync(sensor.Id, variableId, bucketTime);
                if (existing is not null) continue;

                await aggregateRepo.CreateAsync(aggregate);
            }
        }

        var deleted = await readingRepo.DeleteOlderThanAsync(DateTime.UtcNow.AddHours(-24));
        _logger.LogInformation("✅ Aggregates computed. Deleted {Count} old readings.", deleted);
    }
}
