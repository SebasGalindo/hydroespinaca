using SensorService.Domain.Entities;
using SensorService.Domain.ValueObjects;

namespace SensorService.Domain.Interfaces;

/// <summary>
/// Servicio de dominio responsable de crear entidades de agregación
/// a partir de datos estadísticos calculados en una ventana temporal.
/// </summary>
public interface IAggregationService
{
    /// <summary>
    /// Crea una entidad de agregación con identificador único compuesto.
    /// </summary>
    /// <param name="sensorCode">Código del sensor origen de las lecturas.</param>
    /// <param name="variableCode">Código de la variable medida.</param>
    /// <param name="window">Ventana temporal de la agregación.</param>
    /// <param name="data">Datos estadísticos agregados (promedio, mín, máx, conteo).</param>
    /// <returns>Una nueva entidad <see cref="Aggregate"/> con su ID generado.</returns>
    Aggregate CreateAggregate(string sensorCode, string variableCode, TimeWindow window, AggregateData data);
}