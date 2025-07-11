using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using MQTTnet;
using SensorService.Domain.Config;
using SensorService.Domain.Entities;
using SensorService.Domain.Interfaces;
using System.Buffers;
using System.Text;

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

        var factory = new MqttClientFactory(); // 👈 esto asumes que ya existe como helper
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
            var payload = e.ApplicationMessage.Payload.IsEmpty
                ? string.Empty
                : Encoding.UTF8.GetString(e.ApplicationMessage.Payload.ToArray());

            Console.WriteLine($"📥 MQTT → Topic: {topic} | Payload: {payload}");

            var parts = topic.Split('/');
            if (parts.Length < 3)
            {
                Console.WriteLine($"❌ MQTT topic inválido: {topic}");
                return;
            }

            var physicalId = parts[1];   // ej: "snh0016"
            var variableId = parts[2];   // ej: "ph", "temp", etc.

            if (!double.TryParse(payload, out var value))
            {
                Console.WriteLine($"⚠️ Valor inválido: {payload}");
                return;
            }
            try
            {
                // Buscar el sensor que tenga ese PhysicalId y contenga esa variable
                var sensors = await _sensorRepository.GetAllAsync();
                var matchedSensor = sensors.FirstOrDefault(s =>
                    s.PhysicalId == physicalId && s.Variables.Contains(variableId));

                if (matchedSensor == null)
                {
                    Console.WriteLine($"⚠️ Sensor no encontrado para: {physicalId} - {variableId}");
                    return;
                }

                var reading = new Reading
                {
                    SensorId = matchedSensor.Id!,
                    VariableId = variableId,
                    Value = value,
                    Timestamp = DateTime.UtcNow
                };

                await _readingRepository.CreateAsync(reading);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error procesando lectura MQTT: {ex.Message}");
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
