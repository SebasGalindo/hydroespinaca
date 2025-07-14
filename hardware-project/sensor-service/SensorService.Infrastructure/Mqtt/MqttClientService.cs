using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using MQTTnet;
using SensorService.Domain.Config;
using SensorService.Domain.Entities;
using SensorService.Domain.Interfaces;
using System.Buffers;
using System.Text;
using SensorService.Application.DTOs.Mqtt;

namespace SensorService.Infrastructure.Mqtt;

public class MqttClientService : BackgroundService
{
    private readonly IMqttClient _client;
    private readonly MqttClientOptions _options;
    private readonly IReadingRepository _readingRepository;
    private readonly ISensorRepository _sensorRepository;
    private readonly ISensorAlertRepository _alertRepository;
    private readonly IAggregateRepository _aggregateRepository;
    private readonly IVariableRepository _variableRepository;

    public MqttClientService(
        IReadingRepository readingRepository,
        ISensorRepository sensorRepository,
        IOptions<MqttSettings> mqttOptions,
        IAggregateRepository aggregateRepository,
        ISensorAlertRepository alertRepository,
        IVariableRepository variableRepository
        )
    {
        _readingRepository = readingRepository;
        _sensorRepository = sensorRepository;
        _aggregateRepository = aggregateRepository;
        _alertRepository = alertRepository;
        _variableRepository = variableRepository;

        var config = mqttOptions.Value;

        var factory = new MqttClientFactory();
        _client = factory.CreateMqttClient();

        _options = new MqttClientOptionsBuilder()
           .WithClientId(config.ClientId)
           .WithTcpServer(config.Host, config.Port)
           .WithCredentials(config.Username, config.Password)
           .WithCleanSession()
           .Build();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _client.ApplicationMessageReceivedAsync += async e =>
        {
            var topic = e.ApplicationMessage.Topic;

            if (!topic.EndsWith("/readings"))
            {
                Console.WriteLine($"⚠️ Topic ignorado: {topic}");
                return;
            }

            var payload = Encoding.UTF8.GetString(e.ApplicationMessage.Payload.ToArray());
            Console.WriteLine($"📥 MQTT → Topic: {topic} | Payload: {payload}");

            try
            {
                var dto = System.Text.Json.JsonSerializer.Deserialize<ReadingBatchDto>(payload, new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (dto == null || dto.Readings == null || dto.Readings.Count == 0)
                {
                    Console.WriteLine("⚠️ JSON vacío o mal formado (Readings vacíos).");
                    return;
                }

                var allSensors = await _sensorRepository.GetAllAsync();

                // Mark as Esp32 Offline alerts if the ESP32 sent data again
                var anySensor = allSensors.FirstOrDefault(s => s.Esp32Id == dto.Esp32Id);
                if (anySensor != null)
                {
                    var activeOfflineAlerts = await _alertRepository.GetBySensorIdAsync(anySensor.Id!);
                    var unresolvedEsp32OfflineAlerts = activeOfflineAlerts
                        .Where(a => a.Type == "Esp32Offline" && !a.Acknowledged)
                        .ToList();

                    foreach (var alert in unresolvedEsp32OfflineAlerts)
                    {
                        await _alertRepository.AcknowledgeAsync(alert.Id);
                        Console.WriteLine($"✅ Alerta 'Esp32Offline' resuelta automáticamente para ESP32: {dto.Esp32Id}");
                    }
                }

                foreach (var reading in dto.Readings)
                {
                    var matchedSensor = allSensors.FirstOrDefault(s =>
                        s.PhysicalId == reading.PhysicalId &&
                        s.Variables.Contains(reading.VariableId));

                    if (matchedSensor == null)
                    {
                        Console.WriteLine($"⚠️ No match: {reading.PhysicalId} - {reading.VariableId}");
                        continue;
                    }


                    // Validation against agronomic thresholds
                    var variable = await _variableRepository.GetByIdAsync(reading.VariableId);
                    if (variable is not null &&
                    (reading.Value < variable.MinValue || reading.Value > variable.MaxValue))
                    {
                        var threshold = reading.Value < variable.MinValue ? variable.MinValue : variable.MaxValue;

                        await _alertRepository.CreateAsync(new SensorAlert
                        {
                            SensorId = matchedSensor.Id!,
                            Type = "OutOfRange",
                            Value = reading.Value,
                            Threshold = threshold,
                            Timestamp = dto.Timestamp,
                            Severity = "warning",
                            Message = $"Valor {reading.Value} fuera del rango permitido [{variable.MinValue} - {variable.MaxValue}]",
                            Acknowledged = false
                        });
                    }

                    // Comparison with last aggregate
                    var lastAggregate = await _aggregateRepository
                        .GetBySensorAndVariableAsync(matchedSensor.Id!, reading.VariableId, DateTime.UtcNow.AddMinutes(-30), DateTime.UtcNow);

                    var latest = lastAggregate.OrderByDescending(x => x.Timestamp).FirstOrDefault();
                    if (latest != null)
                    {
                        var diff = Math.Abs(reading.Value - latest.Avg);
                        var threshold = latest.Avg * 0.2;

                        if (diff > threshold)
                        {
                            await _alertRepository.CreateAsync(new SensorAlert
                            {
                                SensorId = matchedSensor.Id!,
                                Type = "Anomaly",
                                Value = reading.Value,
                                Threshold = latest.Avg,
                                Timestamp = dto.Timestamp,
                                Severity = "info",
                                Message = $"Valor anómalo: {reading.Value} difiere significativamente del promedio anterior {latest.Avg:F2}",
                                Acknowledged = false
                            });
                        }
                    }


                    var entity = new Reading
                    {
                        SensorId = matchedSensor.Id!,
                        VariableId = reading.VariableId,
                        Value = reading.Value,
                        Timestamp = dto.Timestamp
                    };

                    await _readingRepository.CreateAsync(entity);
                }
                var expectedSensors = allSensors
                .Where(s => s.Esp32Id == dto.Esp32Id)
                .SelectMany(s => s.Variables.Select(v => new { s.Id, s.PhysicalId, VariableId = v }))
                .ToList();

                var receivedKeys = dto.Readings
                    .Select(r => $"{r.PhysicalId}-{r.VariableId}")
                    .ToHashSet();

                foreach (var expected in expectedSensors)
                {
                    var key = $"{expected.PhysicalId}-{expected.VariableId}";
                    if (!receivedKeys.Contains(key))
                    {
                        await _alertRepository.CreateAsync(new SensorAlert
                        {
                            SensorId = expected.Id,
                            Type = "InactiveSensor",
                            Value = 0,
                            Threshold = 0,
                            Timestamp = dto.Timestamp,
                            Severity = "critical",
                            Message = $"No se recibió lectura esperada de {expected.PhysicalId} - {expected.VariableId}",
                            Acknowledged = false
                        });
                    }
                }

                Console.WriteLine("✅ Lecturas agrupadas procesadas.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error parsing JSON: {ex.Message}");
            }
        };

        _client.ConnectedAsync += async e =>
        {
            Console.WriteLine("✅ Conectado a MQTT.");
            await _client.SubscribeAsync(new MqttTopicFilterBuilder()
                .WithTopic("sensor/#")
                .Build());
        };

        _client.DisconnectedAsync += async e =>
        {
            Console.WriteLine("🔌 Desconectado. Reintentando en 5s...");
            await Task.Delay(TimeSpan.FromSeconds(5));
            try
            {
                await _client.ConnectAsync(_options, CancellationToken.None);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Reintento fallido: {ex.Message}");
            }
        };

        await _client.ConnectAsync(_options, stoppingToken);
    }

}
