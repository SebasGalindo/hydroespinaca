using HydroEspinaca.Shared.Constants;
using HydroEspinaca.Shared.DTOs.Mqtt;
using Microsoft.Extensions.Logging;
using SensorService.Application.Interfaces.UseCases.ProcessReadingBatch;
using SensorService.Domain.Interfaces;
using System.Text;
using System.Text.Json;

namespace SensorService.Application.UseCases.ProcessReadingBatch;
public class MqttMessageHandler : IMqttMessageHandler
{
    private readonly IProcessReadingBatchUseCase _useCase;
    private readonly ILogger<MqttMessageHandler> _logger;

    public MqttMessageHandler(IProcessReadingBatchUseCase useCase, ILogger<MqttMessageHandler> logger)
    {
        _useCase = useCase;
        _logger = logger;
    }

    public async Task HandleAsync(string topic, byte[] payload, CancellationToken cancellationToken)
    {
        if (!topic.EndsWith("/readings")) return;

        var json = Encoding.UTF8.GetString(payload);
        var dto = JsonSerializer.Deserialize<ReadingBatchDto>(json, JsonConstants.SerializerOptions.CaseInsensitive);

        if (dto is null)
        {
            _logger.LogWarning("DTO es null. Payload: {Payload}", json);
            return;
        }

        var result = await _useCase.ExecuteAsync(dto, cancellationToken);

        if (result.IsSuccess)
            _logger.LogInformation("✅ Lecturas: {Total}, Alertas: {Alerts}", result.Value.TotalReadings, result.Value.TotalAlerts);
        else
            _logger.LogError("❌ Error: {Error}", result.Error);
    }
}
