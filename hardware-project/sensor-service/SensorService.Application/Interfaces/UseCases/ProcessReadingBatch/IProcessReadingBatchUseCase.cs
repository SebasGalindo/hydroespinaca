using HydroEspinaca.Shared.DTOs.Mqtt;
using SensorService.Application.UseCases.ProcessReadingBatch;

namespace SensorService.Application.Interfaces.UseCases.ProcessReadingBatch;

/// <summary>
/// Contrato del caso de uso principal para procesar un lote de lecturas de sensores.
/// Orquesta el flujo completo: validación, emparejamiento con sensores, generación de alertas,
/// persistencia y notificaciones críticas.
/// </summary>
public interface IProcessReadingBatchUseCase
{
    /// <summary>
    /// Ejecuta el procesamiento completo de un lote de lecturas recibido de un ESP32 vía MQTT.
    /// </summary>
    /// <param name="dto">DTO del lote de lecturas con identificador del ESP32, marca de tiempo y lecturas individuales.</param>
    /// <param name="cancellationToken">Token de cancelación para la operación asíncrona.</param>
    /// <returns>Resultado con la cantidad de lecturas procesadas y alertas generadas, o mensaje de error.</returns>
    Task<Result<ProcessReadingBatchOutput>> ExecuteAsync(ReadingBatchDto dto, CancellationToken cancellationToken = default);
}