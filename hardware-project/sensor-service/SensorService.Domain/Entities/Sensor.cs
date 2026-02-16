using HydroEspinaca.Shared.Abstractions;
using HydroEspinaca.Shared.Enums;
namespace SensorService.Domain.Entities;

/// <summary>
/// Representa un sensor físico en el sistema de cultivo hidropónico.
/// Cada sensor está asociado a un nodo ESP32 y mide una o más variables ambientales.
/// </summary>
public class Sensor : IIdentifiableMutable
{
    /// <summary>
    /// Identificador único del sensor asignado por la base de datos.
    /// </summary>
    public string Id { get; private set; } = default!;

    /// <summary>
    /// Código único del sensor utilizado para identificarlo en el sistema (ej: "dht22_01").
    /// </summary>
    public string Code { get; set; } = default!;

    /// <summary>
    /// Identificador físico del sensor en el hardware (ej: dirección I2C, pin GPIO).
    /// </summary>
    public string PhysicalId { get; set; } = default!;

    /// <summary>
    /// Ubicación física del sensor dentro del sistema de cultivo hidropónico.
    /// </summary>
    public string Location { get; set; } = default!;

    /// <summary>
    /// Identificador del nodo ESP32 al que está conectado este sensor.
    /// </summary>
    public string Esp32Id { get; set; } = default!;

    /// <summary>
    /// Estado actual del sensor (Activo, Inactivo, etc.).
    /// </summary>
    public SensorStatus Status { get; set; } = SensorStatus.Active;

    /// <summary>
    /// Frecuencia de muestreo del sensor expresada en segundos.
    /// </summary>
    public int SamplingFrequency { get; set; }

    /// <summary>
    /// Lista de códigos de variables ambientales que mide este sensor.
    /// </summary>
    public List<string> Variables { get; set; } = new();

    /// <summary>
    /// Indica si se permite que el sensor no reporte datos sin generar alertas de inactividad.
    /// </summary>
    public bool AllowMissing { get; set; } = false;

    /// <summary>
    /// Fecha y hora de creación del registro del sensor (en UTC).
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Establece el identificador único del sensor.
    /// </summary>
    /// <param name="id">Identificador único asignado por la base de datos.</param>
    public void SetId(string id) => Id = id;
}
