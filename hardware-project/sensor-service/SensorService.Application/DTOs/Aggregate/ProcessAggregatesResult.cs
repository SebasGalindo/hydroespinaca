namespace SensorService.Application.DTOs.Aggregate;
public record ProcessAggregatesResult(
    int ProcessedCount,
    int SkippedCount,
    int DeletedReadingsCount);

public record SensorVariableProcessResult(bool WasProcessed);