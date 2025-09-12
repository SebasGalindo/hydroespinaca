namespace HydroEspinaca.Shared.Constants;

public static class AggregationConstants
{
    /// <summary>
    /// Aggregation window size in minutes
    /// </summary>
    public const int AggregationWindowMinutes = 20;
    
    /// <summary>
    /// Worker execution interval in minutes
    /// </summary>
    public const int WorkerIntervalMinutes = 20;
    
    /// <summary>
    /// Reading retention period in hours
    /// </summary>
    public const int ReadingRetentionHours = 24;
}