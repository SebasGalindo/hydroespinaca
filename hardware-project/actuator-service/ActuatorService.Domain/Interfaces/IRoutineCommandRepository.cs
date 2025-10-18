using ActuatorService.Domain.Entities;

namespace ActuatorService.Domain.Interfaces;

public interface IRoutineCommandRepository
{
    Task<RoutineCommand?> GetByCommandIdAsync(string commandId);
    Task<RoutineCommand?> GetRunningByActuatorCodeAsync(string actuatorCode);
    Task<List<RoutineCommand>> GetByActuatorCodeAsync(string actuatorCode);
    Task<List<RoutineCommand>> GetAllAsync();
    Task<List<RoutineCommand>> GetByEsp32IdAsync(string esp32Id);
    Task AddAsync(RoutineCommand routineCommand);
    Task UpdateAsync(RoutineCommand routineCommand);
    Task DeleteAsync(string id);
    Task<long> DeleteOlderThanAsync(DateTime cutoffDate);
    Task<ActuatorAnalyticsData> GetActuatorAnalyticsAsync(DateTime startDate, DateTime endDate, string view);
}

public record ActuatorAnalyticsData
{
    public List<TimelineData> Timeline { get; init; } = new();
    public List<TotalDurationData> TotalDurationByActuator { get; init; } = new();
    public List<ActiveTimeProportionData> ActiveTimeProportion { get; init; } = new();
}

public record TimelineData
{
    public DateTime Timestamp { get; init; }
    public string ActuatorCode { get; init; } = string.Empty;
    public double TotalDurationSeconds { get; init; }
    public int ActivationCount { get; init; }
}

public record TotalDurationData
{
    public string ActuatorCode { get; init; } = string.Empty;
    public double TotalDurationSeconds { get; init; }
    public int ActivationCount { get; init; }
}

public record ActiveTimeProportionData
{
    public string ActuatorCode { get; init; } = string.Empty;
    public double Percentage { get; init; }
}