using HydroEspinaca.Shared.Abstractions;

namespace SensorService.Domain.Entities;

/// <summary>
/// Representa datos agregados (promedio, mínimo, máximo, conteo) de lecturas de sensores
/// para una ventana de tiempo específica. Se usa para análisis de tendencias y reportes.
/// </summary>
public class Aggregate : IIdentifiableMutable
{
    /// <summary>
    /// Identificador único compuesto del agregado (sensorCode-variableCode-timestamp).
    /// </summary>
    public string Id { get; private set; } = default!;

    /// <summary>
    /// Código del sensor origen de las lecturas agregadas.
    /// </summary>
    public string SensorCode { get; set; } = default!;

    /// <summary>
    /// Código de la variable ambiental agregada (ej: temperature, humidity, ph).
    /// </summary>
    public string VariableCode { get; set; } = default!;

    /// <summary>
    /// Promedio de los valores de las lecturas en la ventana temporal.
    /// </summary>
    public double Avg { get; set; }

    /// <summary>
    /// Valor mínimo registrado en la ventana temporal.
    /// </summary>
    public double Min { get; set; }

    /// <summary>
    /// Valor máximo registrado en la ventana temporal.
    /// </summary>
    public double Max { get; set; }

    /// <summary>
    /// Cantidad de lecturas incluidas en la agregación.
    /// </summary>
    public int Count { get; set; }

    /// <summary>
    /// Marca temporal del fin de la ventana de agregación (en UTC).
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Establece el identificador único del agregado.
    /// </summary>
    /// <param name="id">Identificador único compuesto (sensorCode-variableCode-timestamp).</param>
    public void SetId(string id) => Id = id;
}
