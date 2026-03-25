using SensorService.Domain.Entities;

namespace SensorService.Domain.Interfaces;

/// <summary>
/// Servicio de dominio encargado de resolver alertas activas de sensores
/// cuando los valores de las lecturas vuelven al rango óptimo.
/// </summary>
public interface IAlertResolutionService
{
    /// <summary>
    /// Verifica si la lectura actual está dentro del rango óptimo y resuelve
    /// la alerta activa correspondiente si es así.
    /// </summary>
    /// <param name="reading">Lectura actual del sensor.</param>
    /// <param name="variable">Variable con los rangos óptimos definidos.</param>
    /// <param name="timestamp">Momento de la resolución.</param>
    Task ResolveOutOfRangeAlertsAsync(Reading reading, Variable variable, DateTime timestamp);
}