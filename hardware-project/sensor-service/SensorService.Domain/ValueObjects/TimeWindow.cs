using HydroEspinaca.Shared.Constants;
using SensorService.Domain.Exceptions;

namespace SensorService.Domain.ValueObjects;

public record TimeWindow
{
    public DateTime Start { get; }
    public DateTime End { get; }

    private TimeWindow(DateTime start, DateTime end)
    {
        Start = start;
        End = end;
    }

    public static TimeWindow CreateAggregationWindow(DateTime referenceTime)
    {
        var bucketTime = new DateTime(
            referenceTime.Year, referenceTime.Month, referenceTime.Day,
            referenceTime.Hour, (referenceTime.Minute / AggregationConstants.AggregationWindowMinutes) * AggregationConstants.AggregationWindowMinutes, 0,
            DateTimeKind.Utc
        );

        return new TimeWindow(bucketTime.AddMinutes(-AggregationConstants.AggregationWindowMinutes), bucketTime);
    }

    public static TimeWindow Create(DateTime start, DateTime end)
    {
        if (start >= end)
            throw new InvalidTimeWindowException("Start time must be before end time");

        return new TimeWindow(start, end);
    }
}
