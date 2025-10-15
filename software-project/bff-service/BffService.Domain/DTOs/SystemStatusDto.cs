using HydroEspinaca.Shared.DTOs.Readings;
using HydroEspinaca.Shared.DTOs.Actuator;

namespace BffService.Domain.DTOs;

/// <summary>
/// Consolidated system status combining sensor readings and actuator job information.
/// This DTO is specific to the BFF layer and aggregates data from multiple microservices.
/// </summary>
public class SystemStatusDto
{
    /// <summary>
    /// Latest sensor readings with enriched variable information
    /// </summary>
    public EnrichedLatestReadingsDto Readings { get; set; } = new();

    /// <summary>
    /// Current job status and queue information
    /// </summary>
    public JobStatusDto JobStatus { get; set; } = new();

    /// <summary>
    /// Job execution statistics across all devices
    /// </summary>
    public JobExecutionStatsDto Stats { get; set; } = new();

    /// <summary>
    /// Information about internal scheduled routines
    /// </summary>
    public List<InternalRoutineInfoDto> InternalRoutines { get; set; } = new();
}
