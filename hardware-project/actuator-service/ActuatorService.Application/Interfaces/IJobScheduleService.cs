using ActuatorService.Application.DTOs;
using HydroEspinaca.Shared.DTOs.Actuator;

namespace ActuatorService.Application.Interfaces;

public interface IJobScheduleService
{
    Task<JobStatusDto> GetJobStatusAsync(string? esp32Id = null);
    Task<JobScheduleDto> CreateJobScheduleAsync(List<ResolvedRoutineDto> resolvedRoutines);
    Task<JobScheduleDto> CreateJobScheduleAsync(List<RoutineCommandDto> routines); // Legacy - throws NotSupportedException
    Task UpdateChannelStatusAsync(string commandId, string status);
    Task<List<string>> GetActiveEsp32IdsAsync();
    Task ClearJobScheduleAsync(string? esp32Id = null);
}