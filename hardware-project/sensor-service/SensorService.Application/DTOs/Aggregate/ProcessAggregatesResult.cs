namespace SensorService.Application.DTOs.Aggregate;

/// <summary>
/// Resultado del procesamiento periódico de agregación de lecturas de sensores.
/// </summary>
/// <param name="ProcessedCount">Cantidad de agregados procesados exitosamente.</param>
/// <param name="SkippedCount">Cantidad de agregados omitidos (ya existentes o sin datos).</param>
/// <param name="DeletedReadingsCount">Cantidad de lecturas antiguas eliminadas durante la limpieza.</param>
public record ProcessAggregatesResult(
    int ProcessedCount,
    int SkippedCount,
    int DeletedReadingsCount);

/// <summary>
/// Resultado del procesamiento de una combinación sensor-variable individual.
/// </summary>
/// <param name="WasProcessed">Indica si el agregado fue procesado o fue omitido.</param>
public record SensorVariableProcessResult(bool WasProcessed);