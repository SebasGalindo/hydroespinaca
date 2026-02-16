namespace SensorService.Domain.ValueObjects;

/// <summary>
/// Objeto de valor que contiene los datos estadísticos agregados (promedio, mínimo, máximo, conteo)
/// calculados a partir de un conjunto de valores de lecturas de sensores.
/// </summary>
public record AggregateData
{
    /// <summary>
    /// Promedio de los valores de las lecturas.
    /// </summary>
    public double Average { get; }

    /// <summary>
    /// Valor mínimo registrado en el conjunto de lecturas.
    /// </summary>
    public double Min { get; }

    /// <summary>
    /// Valor máximo registrado en el conjunto de lecturas.
    /// </summary>
    public double Max { get; }

    /// <summary>
    /// Cantidad de lecturas incluidas en el cálculo.
    /// </summary>
    public int Count { get; }

    private AggregateData(double average, double min, double max, int count)
    {
        Average = average;
        Min = min;
        Max = max;
        Count = count;
    }

    /// <summary>
    /// Calcula los datos agregados a partir de una colección de valores numéricos.
    /// </summary>
    /// <param name="values">Colección de valores de lecturas de sensores.</param>
    /// <returns>Datos agregados con promedio, mínimo, máximo y conteo.</returns>
    /// <exception cref="ArgumentException">Si la colección de valores está vacía.</exception>
    public static AggregateData FromValues(IEnumerable<double> values)
    {
        var valuesList = values.ToList();

        if (valuesList.Count == 0)
            throw new ArgumentException("Cannot create aggregate from empty values");

        return new AggregateData(
            valuesList.Average(),
            valuesList.Min(),
            valuesList.Max(),
            valuesList.Count
        );
    }
}