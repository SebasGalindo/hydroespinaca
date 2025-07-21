namespace HydroEspinaca.Shared.DTOs.Mqtt;

public class ProcessReadingBatchOutput
{
    public int TotalReadings { get; init; }
    public int TotalAlerts { get; init; }

    public ProcessReadingBatchOutput(int totalReadings, int totalAlerts)
    {
        TotalReadings = totalReadings;
        TotalAlerts = totalAlerts;
    }
}
