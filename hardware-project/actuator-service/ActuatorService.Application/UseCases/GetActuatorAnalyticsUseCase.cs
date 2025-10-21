using ActuatorService.Domain.Interfaces;
using HydroEspinaca.Shared.DTOs.Analytics;
using Microsoft.Extensions.Logging;

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

    public async Task<ActuatorAnalyticsResponse> ExecuteAsync(ActuatorAnalyticsRequest request)
    {
        _logger.LogInformation(
            "Getting actuator analytics from {StartDate} to {EndDate} with view {View}",
            request.StartDate, request.EndDate, request.View);

        var analyticsData = await _routineCommandRepository.GetActuatorAnalyticsAsync(
            request.StartDate,
            request.EndDate,
            request.View);

        var response = new ActuatorAnalyticsResponse
        {
            Timeline = analyticsData.Timeline.Select(t => new ActuatorTimelineItem
            {
                Timestamp = t.Timestamp,
                ActuatorCode = t.ActuatorCode,
                TotalDurationSeconds = t.TotalDurationSeconds,
                ActivationCount = t.ActivationCount
            }).ToList(),
            TotalDurationByActuator = analyticsData.TotalDurationByActuator.Select(td => new ActuatorTotalDurationItem
            {
                ActuatorCode = td.ActuatorCode,
                TotalDurationSeconds = td.TotalDurationSeconds,
                ActivationCount = td.ActivationCount
            }).ToList(),
            ActiveTimeProportion = analyticsData.ActiveTimeProportion.Select(atp => new ActuatorActiveTimeProportionItem
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
