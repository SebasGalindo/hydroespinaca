using HydroEspinaca.Shared.Constants;
using SensorService.Domain.Exceptions;

namespace SensorService.Domain.ValueObjects;

/// <summary>
/// Objeto de valor que representa una ventana temporal con inicio y fin.
/// Se utiliza para definir períodos de agregación de lecturas de sensores.
/// </summary>
public record TimeWindow
{
    /// <summary>
    /// Fecha y hora de inicio de la ventana temporal.
    /// </summary>
    public DateTime Start { get; }

    /// <summary>
    /// Fecha y hora de fin de la ventana temporal.
    /// </summary>
    public DateTime End { get; }

    private TimeWindow(DateTime start, DateTime end)
    {
        Start = start;
        End = end;
    }

    /// <summary>
    /// Crea una ventana de agregación alineada al intervalo configurado (ej: cada 15 minutos).
    /// </summary>
    /// <param name="referenceTime">Tiempo de referencia para calcular el bucket de agregación.</param>
    /// <returns>Una ventana temporal alineada al intervalo de agregación anterior.</returns>
    public static TimeWindow CreateAggregationWindow(DateTime referenceTime)
    {
        var bucketTime = new DateTime(
            referenceTime.Year, referenceTime.Month, referenceTime.Day,
            referenceTime.Hour, (referenceTime.Minute / AggregationConstants.AggregationWindowMinutes) * AggregationConstants.AggregationWindowMinutes, 0,
            DateTimeKind.Utc
        );

        return new TimeWindow(bucketTime.AddMinutes(-AggregationConstants.AggregationWindowMinutes), bucketTime);
    }

    /// <summary>
    /// Crea una ventana temporal personalizada con validación de fechas.
    /// </summary>
    /// <param name="start">Inicio de la ventana temporal.</param>
    /// <param name="end">Fin de la ventana temporal.</param>
    /// <returns>Una nueva instancia de <see cref="TimeWindow"/>.</returns>
    /// <exception cref="InvalidTimeWindowException">Si la fecha de inicio no es anterior a la fecha de fin.</exception>
    public static TimeWindow Create(DateTime start, DateTime end)
    {
        if (start >= end)
            throw new InvalidTimeWindowException("Start time must be before end time");

        return new TimeWindow(start, end);
    }
}
