using HydroEspinaca.Shared.Constants;
using HydroEspinaca.Shared.Enums;
using Microsoft.Extensions.Logging;
using SensorService.Application.DTOs.Aggregate;
using SensorService.Application.Interfaces.UseCases.AggregateWorker;
using SensorService.Domain.Interfaces;
using SensorService.Domain.ValueObjects;

namespace SensorService.Application.UseCases;

public class ProcessAggregatesUseCase : IProcessAggregatesUseCase
{
    private readonly ISensorRepository _sensorRepository;
    private readonly IReadingRepository _readingRepository;
    private readonly IAggregateRepository _aggregateRepository;
    private readonly IAggregationService _aggregationService;
    private readonly ILogger<ProcessAggregatesUseCase> _logger;

    public ProcessAggregatesUseCase(
        ISensorRepository sensorRepository,
        IReadingRepository readingRepository,
        IAggregateRepository aggregateRepository,
        IAggregationService aggregationService,
        ILogger<ProcessAggregatesUseCase> logger)
    {
        _sensorRepository = sensorRepository;
        _readingRepository = readingRepository;
        _aggregateRepository = aggregateRepository;
        _aggregationService = aggregationService;
        _logger = logger;
    }

    public async Task<ProcessAggregatesResult> ExecuteAsync(DateTime referenceTime)
    {
        var window = TimeWindow.CreateAggregationWindow(referenceTime);
        _logger.LogDebug("📅 Aggregation window: {Start} to {End}", window.Start, window.End);

        var sensors = await _sensorRepository.GetAllAsync();

        var expectedSensors = sensors
           .Where(s => s.Status == SensorStatus.Active)
           .ToList();

        _logger.LogDebug("🔍 Active sensors: {Count}", expectedSensors.Count);

        var processedAggregates = 0;
        var skippedAggregates = 0;

        foreach (var sensor in expectedSensors)
        {
            foreach (var variableId in sensor.Variables)
            {
                var result = await ProcessSensorVariableAsync(sensor.Code, variableId, window);

                if (result.WasProcessed)
                    processedAggregates++;
                else
                    skippedAggregates++;
            }
        }

        var deletedReadings = await CleanupOldReadingsAsync();

        _logger.LogInformation(
            "✅ Processed {ProcessedCount} aggregates, skipped {SkippedCount}. Deleted {DeletedCount} old readings.",
            processedAggregates, skippedAggregates, deletedReadings);

        return new ProcessAggregatesResult(processedAggregates, skippedAggregates, deletedReadings);
    }

    private async Task<SensorVariableProcessResult> ProcessSensorVariableAsync(
        string sensorCode,
        string variableId,
        TimeWindow window)
    {
        _logger.LogDebug("🔎 Processing sensorCode={SensorCode}, variableId={VariableId}", sensorCode, variableId);

        // Check if aggregate already exists
        var existing = await _aggregateRepository.GetBySensorAndVariableAndTimestampAsync(
            sensorCode, variableId, window.End);

        if (existing is not null)
        {
            _logger.LogDebug("⏭️  Aggregate already exists for {SensorCode}-{VariableId} at {Timestamp}",
                sensorCode, variableId, window.End);
            return new SensorVariableProcessResult(false);
        }

        // Get readings for the time window
        var readings = await _readingRepository.GetBySensorAndVariableAsync(
            sensorCode, variableId, window.Start, window.End);

        _logger.LogDebug("📊 Found {Count} readings for {SensorCode}-{VariableId} in window",
            readings.Count, sensorCode, variableId);

        if (readings.Count == 0)
        {
            return new SensorVariableProcessResult(false);
        }

        // Create aggregate using domain service
        var values = readings.Select(x => x.Value);
        var aggregateData = AggregateData.FromValues(values);
        var aggregate = _aggregationService.CreateAggregate(sensorCode, variableId, window, aggregateData);

        await _aggregateRepository.CreateAsync(aggregate);

        return new SensorVariableProcessResult(true);
    }

    private async Task<int> CleanupOldReadingsAsync()
    {
        var cutoffTime = DateTime.UtcNow.AddHours(-AggregationConstants.ReadingRetentionHours);
        return await _readingRepository.DeleteOlderThanAsync(cutoffTime);
    }
}