using HydroEspinaca.Shared.DTOs.Mqtt;
using SensorService.Domain.Entities;

namespace SensorService.Application.Interfaces.UseCases.ProcessReadingBatch;

/// <summary>
/// Contrato del caso de uso para emparejar lecturas entrantes con los sensores registrados.
/// Valida que cada lectura corresponda a un sensor válido según su identificador físico y código de variable.
/// </summary>
public interface IMatchReadingsWithSensorsUseCase
{
    /// <summary>
    /// Empareja las lecturas del lote con los sensores registrados en la base de datos.
    /// </summary>
    /// <param name="dto">DTO del lote de lecturas a emparejar.</param>
    /// <returns>Colección de lecturas que fueron emparejadas exitosamente con un sensor.</returns>
    Task<IEnumerable<Reading>> ExecuteAsync(ReadingBatchDto dto);
}