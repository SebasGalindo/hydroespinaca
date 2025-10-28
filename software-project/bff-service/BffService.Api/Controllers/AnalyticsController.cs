using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using BffService.Application.Interfaces;
using BffService.Domain.Interfaces;
using BffService.Domain.DTOs;
using BffService.Api.Helpers;
using HydroEspinaca.Shared.DTOs.Analytics;

namespace BffService.Api.Controllers;

[ApiController]
[Route("analytics")]
[AllowAnonymous] // We'll validate session manually
public class AnalyticsController : BaseAuthenticatedController
{
    private readonly IAnalyticsService _analyticsService;

    public AnalyticsController(
        IAnalyticsService analyticsService,
        ISessionTokenService sessionTokenService,
        IConfiguration configuration,
        ILogger<AnalyticsController> logger)
        : base(sessionTokenService, configuration, logger)
    {
        _analyticsService = analyticsService;
    }

    /// <summary>
    /// Gets environmental aggregates with date range and granularity filtering
    /// </summary>
    [HttpPost("environmental")]
    [ProducesResponseType(typeof(EnvironmentalAggregatesResponse), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> GetEnvironmentalAggregates(
        [FromBody] EnvironmentalAnalyticsRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var session = await ValidateSessionAsync(cancellationToken);
            var result = await _analyticsService.GetEnvironmentalAggregatesAsync(
                request,
                session.AccessToken,
                cancellationToken);

            return Ok(result);
        }
        catch (Exception ex)
        {
            return ControllerExceptionHandler.HandleException(
                ex,
                Logger,
                "getting environmental aggregates");
        }
    }

    /// <summary>
    /// Gets actuator analytics with date range and granularity filtering
    /// </summary>
    [HttpPost("actuators")]
    [ProducesResponseType(typeof(ActuatorAnalyticsResponse), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> GetActuatorAnalytics(
        [FromBody] ActuatorAnalyticsRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var session = await ValidateSessionAsync(cancellationToken);
            var result = await _analyticsService.GetActuatorAnalyticsAsync(
                request,
                session.AccessToken,
                cancellationToken);

            return Ok(result);
        }
        catch (Exception ex)
        {
            return ControllerExceptionHandler.HandleException(
                ex,
                Logger,
                "getting actuator analytics");
        }
    }
}
