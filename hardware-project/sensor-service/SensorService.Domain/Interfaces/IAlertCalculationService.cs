using SensorService.Domain.Entities;

namespace SensorService.Domain.Interfaces;

/// <summary>
/// Servicio de dominio que calcula si una lectura de sensor genera una alerta
/// por estar fuera del rango óptimo definido para la variable ambiental.
/// </summary>
public interface IAlertCalculationService
{
    /// <summary>
    /// Evalúa si el valor de una lectura está fuera del rango óptimo y genera una alerta si corresponde.
    /// </summary>
    /// <param name="reading">Lectura del sensor a evaluar.</param>
    /// <param name="variable">Variable con los rangos óptimos definidos.</param>
    /// <param name="timestamp">Momento de la evaluación.</param>
    /// <returns>Una alerta si el valor está fuera de rango; <c>null</c> si está dentro del rango óptimo.</returns>
    SensorAlert? CalculateOutOfRangeAlert(Reading reading, Variable variable, DateTime timestamp);

    /// <summary>
    /// Determina si un valor numérico se encuentra dentro del rango óptimo de una variable.
    /// </summary>
    /// <param name="value">Valor a evaluar.</param>
    /// <param name="variable">Variable con los rangos óptimos.</param>
    /// <returns><c>true</c> si el valor está dentro del rango óptimo; de lo contrario, <c>false</c>.</returns>
    bool IsValueWithinOptimalRange(double value, Variable variable);
}