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

    public MqttClientService(
        IReadingRepository readingRepository,
        ISensorRepository sensorRepository,
        IOptions<MqttSettings> mqttOptions)
    {
        _readingRepository = readingRepository;
        _sensorRepository = sensorRepository;

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

                    var entity = new Reading
                    {
                        SensorId = matchedSensor.Id!,
                        VariableId = reading.VariableId,
                        Value = reading.Value,
                        Timestamp = dto.Timestamp
                    };

                    await _readingRepository.CreateAsync(entity);
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
