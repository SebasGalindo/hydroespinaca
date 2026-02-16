using HydroEspinaca.Shared.Abstractions;
using HydroEspinaca.Shared.Enums;

namespace SensorService.Domain.Entities;

/// <summary>
/// Clase base abstracta para todas las alertas del sistema.
/// Define las propiedades comunes como timestamp, mensaje, estado de reconocimiento y resolución.
/// </summary>
public abstract class AlertBase : IIdentifiableMutable
{
    /// <summary>
    /// Identificador único de la alerta asignado por la base de datos.
    /// </summary>
    public string Id { get; private set; } = default!;

    /// <summary>
    /// Fecha y hora en que se generó o actualizó la alerta.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Mensaje descriptivo de la alerta indicando la causa y los valores involucrados.
    /// </summary>
    public string Message { get; set; } = default!;

    /// <summary>
    /// Indica si la alerta ha sido reconocida por un usuario del sistema.
    /// </summary>
    public bool Acknowledged { get; set; } = false;

    /// <summary>
    /// Fecha y hora en que se resolvió la alerta. <c>null</c> si la alerta sigue activa.
    /// </summary>
    public DateTime? ResolvedAt { get; set; }

    /// <summary>
    /// Tipo de alerta (campo heredado mantenido por compatibilidad con <see cref="Esp32Alert"/>).
    /// No utilizado en <see cref="SensorAlert"/>.
    /// </summary>
    public AlertType? Type { get; set; }

    /// <summary>
    /// Severidad de la alerta (campo heredado mantenido por compatibilidad con <see cref="Esp32Alert"/>).
    /// No utilizado en <see cref="SensorAlert"/>.
    /// </summary>
    public AlertSeverity? Severity { get; set; }

    /// <summary>
    /// Establece el identificador único de la alerta.
    /// </summary>
    /// <param name="id">Identificador único asignado por la base de datos.</param>
    public void SetId(string id) => Id = id;
}
