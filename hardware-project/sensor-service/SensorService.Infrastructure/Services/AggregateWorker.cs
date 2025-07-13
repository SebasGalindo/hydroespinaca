using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SensorService.Domain.Entities;
using SensorService.Domain.Interfaces;
using SensorService.Infrastructure.Persistence.Models;

namespace SensorService.Infrastructure.Services;

public class AggregateWorker : BackgroundService
{
    private readonly ILogger<AggregateWorker> _logger;
    private readonly ISensorRepository _sensorRepo;
    private readonly IReadingRepository _readingRepo;
    private readonly IAggregateRepository _aggregateRepo;
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(10);

    public AggregateWorker(
        ILogger<AggregateWorker> logger,
        ISensorRepository sensorRepo,
        IReadingRepository readingRepo,
        IAggregateRepository aggregateRepo)
    {
        _logger = logger;
        _sensorRepo = sensorRepo;
        _readingRepo = readingRepo;
        _aggregateRepo = aggregateRepo;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessAggregatesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during aggregation job");
            }

            await Task.Delay(_interval, stoppingToken);
        }
    }

    private async Task ProcessAggregatesAsync()
    {
        var now = DateTime.UtcNow;
        var bucketTime = new DateTime(
            now.Year, now.Month, now.Day, now.Hour,
            (now.Minute / 10) * 10, 0, DateTimeKind.Utc
        );
        var windowStart = bucketTime.AddMinutes(-10);

        var sensors = await _sensorRepo.GetAllAsync();

        foreach (var sensor in sensors)
        {
            foreach (var variableId in sensor.Variables)
            {
                var readings = await _readingRepo.GetBySensorAndVariableAsync(sensor.Id, variableId, windowStart, bucketTime);
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

                var existing = await _aggregateRepo.GetBySensorAndVariableAndTimestampAsync(sensor.Id, variableId, bucketTime);
                if (existing is not null) continue;

                await _aggregateRepo.CreateAsync(aggregate);

            }
        }

        // Cleanup
        var deleted = await _readingRepo.DeleteOlderThanAsync(DateTime.UtcNow.AddHours(-24));
        _logger.LogInformation("Aggregates computed. Deleted {Count} old readings.", deleted);
    }
}
