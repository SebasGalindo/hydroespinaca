using SensorService.Domain.Entities;
using SensorService.Domain.Interfaces;
using SensorService.Domain.ValueObjects;

namespace SensorService.Domain.Services;

/// <summary>
/// Servicio de dominio encargado de crear entidades de agregación a partir de datos estadísticos
/// calculados dentro de una ventana temporal específica.
/// </summary>
public class AggregationService : IAggregationService
{
    /// <summary>
    /// Crea una entidad de agregación con un identificador único compuesto basado en sensor, variable y timestamp.
    /// </summary>
    /// <param name="sensorCode">Código del sensor origen.</param>
    /// <param name="variableCode">Código de la variable medida.</param>
    /// <param name="window">Ventana temporal de agregación.</param>
    /// <param name="data">Datos estadísticos agregados (promedio, mín, máx, conteo).</param>
    /// <returns>Una nueva entidad <see cref="Aggregate"/> con su ID generado.</returns>
    public Aggregate CreateAggregate(
        string sensorCode,
        string variableCode,
        TimeWindow window,
        AggregateData data)
    {
        var aggregateId = GenerateAggregateId(sensorCode, variableCode, window.End);

        var aggregate = new Aggregate
        {
            SensorCode = sensorCode,
            VariableCode = variableCode,
            Avg = data.Average,
            Min = data.Min,
            Max = data.Max,
            Count = data.Count,
            Timestamp = window.End
        };
        aggregate.SetId(aggregateId);
        return aggregate;

    }

    private static string GenerateAggregateId(string sensorCode, string variableCode, DateTime timestamp)
    {
        return $"{sensorCode}-{variableCode}-{timestamp:yyyyMMddHHmm}";
    }
}