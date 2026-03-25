using HydroEspinaca.Shared.DTOs.Mqtt;
using SensorService.Domain.Entities;

namespace SensorService.Application.Interfaces.UseCases.ProcessReadingBatch;

/// <summary>
/// Contrato del caso de uso para generar alertas de sensores inactivos.
/// Actualmente deshabilitado: solo las variables de regulación manual generan alertas basadas en rango óptimo.
/// </summary>
public interface IGenerateInactiveSensorAlertsUseCase
{
    /// <summary>
    /// Ejecuta la verificación de sensores inactivos para un lote de lecturas.
    /// </summary>
    /// <param name="dto">DTO del lote de lecturas recibido del ESP32.</param>
    /// <returns>Colección de alertas generadas por sensores inactivos.</returns>
    Task<IEnumerable<SensorAlert>> ExecuteAsync(ReadingBatchDto dto);
}