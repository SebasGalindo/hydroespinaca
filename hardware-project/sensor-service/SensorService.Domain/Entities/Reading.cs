using HydroEspinaca.Shared.Abstractions;

namespace SensorService.Domain.Entities;

/// <summary>
/// Representa una lectura individual de un sensor, conteniendo el valor medido
/// para una variable específica en un momento determinado.
/// </summary>
public class Reading : IIdentifiableMutable
{
    /// <summary>
    /// Identificador único de la lectura asignado por la base de datos.
    /// </summary>
    public string Id { get; private set; } = default!;

    /// <summary>
    /// Código del sensor que realizó la medición.
    /// </summary>
    public string SensorCode { get; set; } = default!;

    /// <summary>
    /// Código de la variable ambiental medida (ej: temperature, ph, tds).
    /// </summary>
    public string VariableCode { get; set; } = default!;

    /// <summary>
    /// Valor numérico de la medición realizada por el sensor.
    /// </summary>
    public double Value { get; set; }

    /// <summary>
    /// Fecha y hora en que se realizó la lectura (en UTC).
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Establece el identificador único de la lectura.
    /// </summary>
    /// <param name="id">Identificador único asignado por la base de datos.</param>
    public void SetId(string id) => Id = id;

}