using HydroEspinaca.Shared.Abstractions;
using HydroEspinaca.Shared.Enums;

namespace SensorService.Domain.Entities;

/// <summary>
/// Representa una variable ambiental medida por los sensores del sistema hidropónico
/// (ej: temperatura, humedad, pH, conductividad eléctrica, nivel de agua, luminosidad).
/// Define rangos físicos y óptimos para el cultivo de espinaca.
/// </summary>
public class Variable : IIdentifiableMutable
{
    /// <summary>
    /// Identificador único de la variable asignado por la base de datos.
    /// </summary>
    public string Id { get; private set; } = default!;

    /// <summary>
    /// Código único de la variable (ej: temperature, humidity, ph, tds, water_level).
    /// </summary>
    public string Code { get; set; } = default!;

    /// <summary>
    /// Nombre descriptivo de la variable ambiental (ej: "Temperatura", "pH").
    /// </summary>
    public string Name { get; set; } = default!;

    /// <summary>
    /// Unidad de medida de la variable (ej: °C, %, ppm, cm).
    /// </summary>
    public string Unit { get; set; } = default!;

    /// <summary>
    /// Descripción detallada de la variable y su importancia en el cultivo hidropónico.
    /// </summary>
    public string Description { get; set; } = default!;

    /// <summary>
    /// Valor mínimo técnicamente posible para el sensor que mide esta variable.
    /// </summary>
    public double PhysicalMin { get; set; }

    /// <summary>
    /// Valor máximo técnicamente posible para el sensor que mide esta variable.
    /// </summary>
    public double PhysicalMax { get; set; }

    /// <summary>
    /// Valor mínimo del rango óptimo para el crecimiento de espinaca.
    /// </summary>
    public double OptimalMin { get; set; }

    /// <summary>
    /// Valor máximo del rango óptimo para el crecimiento de espinaca.
    /// <c>null</c> si no existe un límite superior definido.
    /// </summary>
    public double? OptimalMax { get; set; }

    /// <summary>
    /// Tipo de variable que categoriza la medición (ej: ambiental, nutriente).
    /// </summary>
    public VariableTypes Type { get; set; }

    /// <summary>
    /// Tipo de regulación de la variable: manual (requiere intervención humana) o automática.
    /// </summary>
    public RegulationType? RegulationType { get; set; }

    /// <summary>
    /// Fecha y hora de la última modificación de la configuración de la variable.
    /// </summary>
    public DateTime LastModified { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Establece el identificador único de la variable.
    /// </summary>
    /// <param name="id">Identificador único asignado por la base de datos.</param>
    public void SetId(string id) => Id = id;
}