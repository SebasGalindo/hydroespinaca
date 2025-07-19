namespace SensorService.Domain.ValueObjects;

public record AggregateData
{
    public double Average { get; }
    public double Min { get; }
    public double Max { get; }
    public int Count { get; }

    private AggregateData(double average, double min, double max, int count)
    {
        Average = average;
        Min = min;
        Max = max;
        Count = count;
    }

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