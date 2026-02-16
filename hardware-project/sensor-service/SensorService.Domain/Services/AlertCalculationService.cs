using SensorService.Domain.Entities;
using SensorService.Domain.Interfaces;

namespace SensorService.Domain.Services;

/// <summary>
/// Servicio de dominio que calcula si una lectura de sensor genera una alerta
/// por estar fuera del rango óptimo definido para la variable.
/// </summary>
public class AlertCalculationService : IAlertCalculationService
{
    /// <summary>
    /// Evalúa si el valor de una lectura está fuera del rango óptimo y genera una alerta si corresponde.
    /// </summary>
    /// <param name="reading">Lectura del sensor a evaluar.</param>
    /// <param name="variable">Variable con los rangos óptimos definidos.</param>
    /// <param name="timestamp">Momento de la evaluación.</param>
    /// <returns>Una alerta si el valor está fuera de rango; <c>null</c> si está dentro del rango óptimo.</returns>
    public SensorAlert? CalculateOutOfRangeAlert(Reading reading, Variable variable, DateTime timestamp)
    {
        var value = reading.Value;

        // Check if value is below OptimalMin
        if (value < variable.OptimalMin)
        {
            return new SensorAlert
            {
                VariableCode = reading.VariableCode,
                Value = value,
                Timestamp = timestamp,
                Message = GetOptimalRangeMessage(value, variable),
                LastSeen = timestamp,
                Acknowledged = false
            };
        }

        // Check if value is above OptimalMax (only if OptimalMax is defined)
        if (variable.OptimalMax.HasValue && value > variable.OptimalMax.Value)
        {
            return new SensorAlert
            {
                VariableCode = reading.VariableCode,
                Value = value,
                Timestamp = timestamp,
                Message = GetOptimalRangeMessage(value, variable),
                LastSeen = timestamp,
                Acknowledged = false
            };
        }

        return null;
    }

    private string GetOptimalRangeMessage(double value, Variable variable)
    {
        if (variable.OptimalMax.HasValue)
        {
            return $"Valor {value} fuera del rango óptimo [{variable.OptimalMin} - {variable.OptimalMax.Value}]";
        }
        else
        {
            return $"Valor {value} por debajo del mínimo óptimo {variable.OptimalMin}";
        }
    }

    /// <summary>
    /// Determina si un valor numérico se encuentra dentro del rango óptimo de una variable.
    /// </summary>
    /// <param name="value">Valor a evaluar.</param>
    /// <param name="variable">Variable con los rangos óptimos.</param>
    /// <returns><c>true</c> si el valor está dentro del rango óptimo; de lo contrario, <c>false</c>.</returns>
    public bool IsValueWithinOptimalRange(double value, Variable variable)
    {
        if (value < variable.OptimalMin)
            return false;

        if (variable.OptimalMax.HasValue && value > variable.OptimalMax.Value)
            return false;

        return true;
    }
}