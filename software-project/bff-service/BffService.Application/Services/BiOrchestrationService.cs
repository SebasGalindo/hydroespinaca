using BffService.Application.Interfaces;
using BffService.Domain.Constants;
using BffService.Domain.DTOs.Bi;
using BffService.Domain.Interfaces;
using BffService.Domain.ValueObjects;
using HydroEspinaca.Shared.DTOs.Actuator;
using HydroEspinaca.Shared.DTOs.Analytics;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace BffService.Application.Services;

/// <summary>
/// Orchestrates BI operations that require data from both actuator-service and bi-service.
/// This keeps bi-service decoupled from actuator-service — BFF is the only orchestrator.
/// </summary>
public class BiOrchestrationService : IBiOrchestrationService
{
    private readonly IBiServiceClient _biClient;
    private readonly IProxyService _proxyService;
    private readonly ILogger<BiOrchestrationService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public BiOrchestrationService(
        IBiServiceClient biClient,
        IProxyService proxyService,
        ILogger<BiOrchestrationService> logger)
    {
        _biClient = biClient;
        _proxyService = proxyService;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    public async Task<OperationalCostResponse> CalculateOperationalCostAsync(
        CalculateOperationalCostBffRequest request,
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Orchestrating operational cost calculation: {From} to {To}",
            request.From, request.To);

        // Step 1: Get actuators (for PowerConsumptionWatts) and analytics (for durations) in parallel
        var actuatorsTask = GetActuatorsFromActuatorServiceAsync(accessToken, cancellationToken);
        var analyticsTask = GetActuatorAnalyticsFromActuatorServiceAsync(
            request.From, request.To, accessToken, cancellationToken);

        await Task.WhenAll(actuatorsTask, analyticsTask);

        var actuators = await actuatorsTask;
        var analytics = await analyticsTask;

        // Step 2: Combine PowerConsumptionWatts with duration data
        var actuatorDurations = CombineActuatorData(actuators, analytics.TotalDurationByActuator);

        if (actuatorDurations.Count == 0)
        {
            _logger.LogInformation("No actuator duration data found for the given date range");
        }

        // Step 3: Send combined data to bi-service for cost calculation
        var serviceRequest = new CalculateOperationalCostServiceRequest
        {
            From = request.From,
            To = request.To,
            ActuatorDurations = actuatorDurations
        };

        return await _biClient.CalculateOperationalCostAsync(
            accessToken, serviceRequest, cancellationToken);
    }

    public async Task<ProfitabilityResponse> CalculateProfitabilityAsync(
        CalculateProfitabilityBffRequest request,
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Orchestrating profitability calculation for production: {ProductionId}",
            request.ProductionRecordId);

        // Step 1: Get production record to know the date range
        var production = await _biClient.GetProductionRecordByIdAsync(
            accessToken, request.ProductionRecordId, cancellationToken);

        if (production is null)
        {
            throw new InvalidOperationException(
                $"Registro de producción '{request.ProductionRecordId}' no encontrado.");
        }

        // Step 2: Get actuator durations (only if automatic energy calculation is enabled)
        List<ActuatorDurationInput> actuatorDurations;
        if (request.IncludeAutomaticEnergyCalculation)
        {
            var actuatorsTask = GetActuatorsFromActuatorServiceAsync(accessToken, cancellationToken);
            var analyticsTask = GetActuatorAnalyticsFromActuatorServiceAsync(
                production.StartDate, production.HarvestDate, accessToken, cancellationToken);

            await Task.WhenAll(actuatorsTask, analyticsTask);

            var actuators = await actuatorsTask;
            var analytics = await analyticsTask;

            actuatorDurations = CombineActuatorData(actuators, analytics.TotalDurationByActuator);
        }
        else
        {
            _logger.LogInformation(
                "Automatic energy calculation disabled — skipping actuator-service calls for production {ProductionId}",
                request.ProductionRecordId);
            actuatorDurations = new List<ActuatorDurationInput>();
        }

        // Step 4: Send combined data to bi-service for profitability calculation
        var serviceRequest = new CalculateProfitabilityServiceRequest
        {
            ProductionRecordId = request.ProductionRecordId,
            ActuatorDurations = actuatorDurations,
            InitialInvestmentCost = request.InitialInvestmentCost
        };

        return await _biClient.CalculateProfitabilityAsync(
            accessToken, serviceRequest, cancellationToken);
    }

    #region Private Helpers

    /// <summary>
    /// Gets all actuators from actuator-service via the proxy.
    /// Returns ActuatorDto list which includes PowerConsumptionWatts.
    /// </summary>
    private async Task<List<ActuatorDto>> GetActuatorsFromActuatorServiceAsync(
        string accessToken, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Fetching actuators from actuator-service");

        var proxyRequest = new ProxyRequest(
            "GET",
            "/actuators",
            new Dictionary<string, string>(),
            null
        );

        var response = await _proxyService.ForwardRequestAsync(
            proxyRequest,
            accessToken,
            BffConstants.Proxy.Services.ActuatorService,
            cancellationToken
        );

        if (response.StatusCode != 200 || string.IsNullOrEmpty(response.Body))
        {
            _logger.LogWarning(
                "Actuator-service returned {StatusCode} when fetching actuators",
                response.StatusCode);
            return new List<ActuatorDto>();
        }

        return JsonSerializer.Deserialize<List<ActuatorDto>>(response.Body, _jsonOptions)
            ?? new List<ActuatorDto>();
    }

    /// <summary>
    /// Gets actuator analytics (durations) from actuator-service via the proxy.
    /// Uses the same POST /commands/analytics endpoint as AnalyticsService.
    /// </summary>
    private async Task<ActuatorAnalyticsResponse> GetActuatorAnalyticsFromActuatorServiceAsync(
        DateTime from, DateTime to, string accessToken, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Fetching actuator analytics from actuator-service: {From} to {To}", from, to);

        var analyticsRequest = new ActuatorAnalyticsRequest(from, to, "daily");
        var requestBody = JsonSerializer.Serialize(analyticsRequest, _jsonOptions);

        var proxyRequest = new ProxyRequest(
            "POST",
            "/commands/analytics",
            new Dictionary<string, string> { { "Content-Type", "application/json" } },
            requestBody
        );

        var response = await _proxyService.ForwardRequestAsync(
            proxyRequest,
            accessToken,
            BffConstants.Proxy.Services.ActuatorService,
            cancellationToken
        );

        if (response.StatusCode != 200 || string.IsNullOrEmpty(response.Body))
        {
            _logger.LogWarning(
                "Actuator-service returned {StatusCode} when fetching analytics",
                response.StatusCode);
            return new ActuatorAnalyticsResponse
            {
                Timeline = new List<ActuatorTimelineItem>(),
                TotalDurationByActuator = new List<ActuatorTotalDurationItem>(),
                ActiveTimeProportion = new List<ActuatorActiveTimeProportionItem>()
            };
        }

        return JsonSerializer.Deserialize<ActuatorAnalyticsResponse>(response.Body, _jsonOptions)
            ?? new ActuatorAnalyticsResponse
            {
                Timeline = new List<ActuatorTimelineItem>(),
                TotalDurationByActuator = new List<ActuatorTotalDurationItem>(),
                ActiveTimeProportion = new List<ActuatorActiveTimeProportionItem>()
            };
    }

    /// <summary>
    /// Combines actuator master data (PowerConsumptionWatts) with analytics duration data.
    /// Only includes actuators that have analytics data.
    /// </summary>
    private List<ActuatorDurationInput> CombineActuatorData(
        List<ActuatorDto> actuators,
        List<ActuatorTotalDurationItem> durations)
    {
        // Build a lookup of actuator Code → PowerConsumptionWatts
        var powerLookup = actuators.ToDictionary(
            a => a.Code,
            a => a.PowerConsumptionWatts,
            StringComparer.OrdinalIgnoreCase
        );

        var result = new List<ActuatorDurationInput>();

        foreach (var duration in durations)
        {
            if (powerLookup.TryGetValue(duration.ActuatorCode, out var watts))
            {
                result.Add(new ActuatorDurationInput
                {
                    ActuatorCode = duration.ActuatorCode,
                    PowerConsumptionWatts = watts,
                    TotalDurationSeconds = duration.TotalDurationSeconds,
                    ActivationCount = duration.ActivationCount
                });
            }
            else
            {
                _logger.LogWarning(
                    "Actuator '{Code}' has analytics data but no PowerConsumptionWatts configured. " +
                    "Using 0W as fallback.",
                    duration.ActuatorCode);

                result.Add(new ActuatorDurationInput
                {
                    ActuatorCode = duration.ActuatorCode,
                    PowerConsumptionWatts = 0,
                    TotalDurationSeconds = duration.TotalDurationSeconds,
                    ActivationCount = duration.ActivationCount
                });
            }
        }

        _logger.LogInformation(
            "Combined {Count} actuator(s) with duration data for cost calculation",
            result.Count);

        return result;
    }

    #endregion
}
