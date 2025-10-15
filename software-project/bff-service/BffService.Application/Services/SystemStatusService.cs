using BffService.Application.Interfaces;
using BffService.Domain.Constants;
using BffService.Domain.DTOs;
using BffService.Domain.Interfaces;
using BffService.Domain.ValueObjects;
using HydroEspinaca.Shared.DTOs.Actuator;
using HydroEspinaca.Shared.DTOs.Readings;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace BffService.Application.Services;

public class SystemStatusService : ISystemStatusService
{
    private readonly IProxyService _proxyService;
    private readonly ILogger<SystemStatusService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public SystemStatusService(IProxyService proxyService, ILogger<SystemStatusService> logger)
    {
        _proxyService = proxyService;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    public async Task<SystemStatusDto> GetSystemStatusAsync(string? accessToken, CancellationToken cancellationToken = default)
    {
        try
        {
            // Create tasks for both services with 2 second timeout
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(2));

            var actuatorTask = GetActuatorStatusAsync(accessToken, cts.Token);
            var sensorTask = GetLatestReadingsAsync(accessToken, cts.Token);

            // Wait for both tasks to complete
            await Task.WhenAll(actuatorTask, sensorTask);

            var actuatorResponse = await actuatorTask;
            var sensorReadings = await sensorTask;

            // Build consolidated system status using shared DTOs directly
            var result = new SystemStatusDto
            {
                Readings = sensorReadings ?? new EnrichedLatestReadingsDto(),
                JobStatus = actuatorResponse.JobStatus ?? new JobStatusDto(),
                Stats = actuatorResponse.Stats ?? new JobExecutionStatsDto(),
                InternalRoutines = actuatorResponse.InternalRoutines ?? new List<InternalRoutineInfoDto>()
            };

            return result;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("System status request timed out after 2 seconds");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting system status");
            throw;
        }
    }

    private async Task<ActuatorStatusResponse> GetActuatorStatusAsync(string? accessToken, CancellationToken cancellationToken)
    {
        var request = new ProxyRequest(
            "GET",
            "/commands/jobs/status",
            new Dictionary<string, string>(),
            null
        );

        var response = await _proxyService.ForwardRequestAsync(
            request,
            accessToken,
            BffConstants.Proxy.Services.ActuatorService,
            cancellationToken
        );

        if (response.StatusCode != 200)
        {
            _logger.LogWarning("Actuator service returned status code {StatusCode}", response.StatusCode);
            return new ActuatorStatusResponse();
        }

        var result = JsonSerializer.Deserialize<ActuatorStatusResponse>(response.Body, _jsonOptions);
        return result ?? new ActuatorStatusResponse();
    }

    private async Task<EnrichedLatestReadingsDto?> GetLatestReadingsAsync(string? accessToken, CancellationToken cancellationToken)
    {
        var request = new ProxyRequest(
            "GET",
            "/readings/latest",
            new Dictionary<string, string>(),
            null
        );

        var response = await _proxyService.ForwardRequestAsync(
            request,
            accessToken,
            BffConstants.Proxy.Services.SensorService,
            cancellationToken
        );

        if (response.StatusCode != 200)
        {
            _logger.LogWarning("Sensor service returned status code {StatusCode}", response.StatusCode);
            return null;
        }

        var result = JsonSerializer.Deserialize<EnrichedLatestReadingsDto>(response.Body, _jsonOptions);
        return result;
    }

    /// <summary>
    /// Internal DTO for deserialization from actuator service /commands/jobs/status endpoint
    /// Uses shared DTOs to avoid duplication
    /// </summary>
    private class ActuatorStatusResponse
    {
        public JobStatusDto? JobStatus { get; set; }
        public JobExecutionStatsDto? Stats { get; set; }
        public List<InternalRoutineInfoDto>? InternalRoutines { get; set; }
    }
}
