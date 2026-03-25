using SensorService.Application.DTOs.Aggregate;

namespace SensorService.Application.Interfaces.UseCases.AggregateWorker;

/// <summary>
/// Contrato del caso de uso para procesar la agregación periódica de lecturas de sensores.
/// Ejecutado por un worker en segundo plano para calcular estadísticas (promedio, mín, máx) y limpiar lecturas antiguas.
/// </summary>
public interface IProcessAggregatesUseCase
{
    /// <summary>
    /// Procesa la agregación de lecturas para todos los sensores activos en la ventana de tiempo especificada.
    /// </summary>
    /// <param name="referenceTime">Tiempo de referencia para calcular la ventana de agregación.</param>
    /// <returns>Resultado con la cantidad de agregados procesados, omitidos y lecturas eliminadas.</returns>
    Task<ProcessAggregatesResult> ExecuteAsync(DateTime referenceTime);
}
