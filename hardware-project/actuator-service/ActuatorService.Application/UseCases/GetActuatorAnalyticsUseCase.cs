using ActuatorService.Domain.Interfaces;
using HydroEspinaca.Shared.DTOs.Analytics;
using Microsoft.Extensions.Logging;
using TimelineData = ActuatorService.Domain.Interfaces.TimelineData;

namespace ActuatorService.Application.UseCases;

public interface IGetActuatorAnalyticsUseCase
{
    Task<ActuatorAnalyticsResponse> ExecuteAsync(ActuatorAnalyticsRequest request);
}

public class GetActuatorAnalyticsUseCase : IGetActuatorAnalyticsUseCase
{
    private readonly IRoutineCommandRepository _routineCommandRepository;
    private readonly ILogger<GetActuatorAnalyticsUseCase> _logger;

    public GetActuatorAnalyticsUseCase(
        IRoutineCommandRepository routineCommandRepository,
        ILogger<GetActuatorAnalyticsUseCase> logger)
    {
        _routineCommandRepository = routineCommandRepository;
        _logger = logger;
    }

    /// <summary>
    /// Timeline automático:
    /// - Si el rango cubre un solo día, los datos del timeline se devuelven sin agrupar (modo raw) para efectos de monitoreo.
    /// - En rangos mayores, el timeline no se devuelve (lista vacía) ya que solo tiene sentido visualizar activaciones individuales en un día.
    /// - Los otros componentes (duración, proporción) siempre usan agregación independientemente del rango.
    /// </summary>
    public async Task<ActuatorAnalyticsResponse> ExecuteAsync(ActuatorAnalyticsRequest request)
    {
        _logger.LogInformation(
            "Getting actuator analytics from {StartDate} to {EndDate} with view {View}",
            request.StartDate, request.EndDate, request.View);

        // Calculate date range difference
        var dateRangeDifference = request.EndDate - request.StartDate;
        var isSingleDay = dateRangeDifference.TotalHours <= 24;

        // Get aggregated data for duration and proportion (always needed)
        var fullAnalyticsData = await _routineCommandRepository.GetActuatorAnalyticsAsync(request);

        List<TimelineData> timelineData;

        if (isSingleDay)
        {
            _logger.LogInformation("Single day range detected. Using raw timeline data (ungrouped) for monitoring purposes.");
            timelineData = await _routineCommandRepository.GetRawTimelineDataAsync(request.StartDate, request.EndDate);
        }
        else
        {
            _logger.LogInformation("Multi-day range detected. Timeline will not be included (only duration and proportion).");
            timelineData = new List<TimelineData>(); // Empty list for multi-day ranges
        }

        var response = new ActuatorAnalyticsResponse
        {
            Timeline = timelineData.Select(t => new ActuatorTimelineItem
            {
                Timestamp = t.Timestamp,
                ActuatorCode = t.ActuatorCode,
                TotalDurationSeconds = t.TotalDurationSeconds,
                ActivationCount = t.ActivationCount
            }).ToList(),
            TotalDurationByActuator = fullAnalyticsData.TotalDurationByActuator.Select(td => new ActuatorTotalDurationItem
            {
                ActuatorCode = td.ActuatorCode,
                TotalDurationSeconds = td.TotalDurationSeconds,
                ActivationCount = td.ActivationCount
            }).ToList(),
            ActiveTimeProportion = fullAnalyticsData.ActiveTimeProportion.Select(atp => new ActuatorActiveTimeProportionItem
            {
                ActuatorCode = atp.ActuatorCode,
                Percentage = atp.Percentage
            }).ToList()
        };

        _logger.LogInformation(
            "Retrieved analytics with {TimelineCount} timeline items, {TotalDurationCount} actuators, {ProportionCount} proportions",
            response.Timeline.Count, response.TotalDurationByActuator.Count, response.ActiveTimeProportion.Count);

        return response;
    }
}
