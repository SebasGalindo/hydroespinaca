using System;
using System.Buffers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using MQTTnet;
using SensorService.Domain.Config;
using SensorService.Domain.Entities;
using SensorService.Domain.Interfaces;

namespace SensorService.Infrastructure.Mqtt;

public class MqttClientService : BackgroundService
{
    private readonly IMqttClient _client;
    private readonly MqttClientOptions _options;
    private readonly ISensorReadingRepository _repository;

    public MqttClientService(
       ISensorReadingRepository repository,
       IOptions<MqttSettings> mqttOptions)
    {
        _repository = repository;
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
            var payload = e.ApplicationMessage.Payload.IsEmpty
                ? string.Empty
                : Encoding.UTF8.GetString(e.ApplicationMessage.Payload.ToArray());

            Console.WriteLine($"📥 Mensaje recibido - Topic: {topic}, Payload: {payload}");

            // Aquí deberías mapear el topic a sensorId y tipo real
            // Este es un ejemplo simplificado:
            var parts = topic.Split('/'); // e.g. sensor/snh0016/temp
            if (parts.Length < 3)
            {
                Console.WriteLine($"❌ Topic no válido: {topic}");
                return;
            }

            var physicalId = parts[1];    // snh0016
            var type = parts[2];          // temp
            var sensorId = $"{physicalId}-{type}"; // match con Sensor.Id

            if (!double.TryParse(payload, out var value))
            {
                Console.WriteLine($"⚠️ Payload inválido (no numérico): {payload}");
                return;
            }

            var reading = new SensorReading
            {
                Id = Guid.NewGuid().ToString(), // O se puede dejar que Mongo lo genere
                SensorId = sensorId,
                Type = type,
                Value = value,
                Timestamp = DateTime.UtcNow
            };

            await _repository.SaveAsync(reading);
        };

        _client.ConnectedAsync += async e =>
        {
            Console.WriteLine("✅ Conectado a MQTT.");
            await _client.SubscribeAsync(new MqttTopicFilterBuilder()
                .WithTopic("sensor/#")
                .Build());
        };

        await _client.ConnectAsync(_options, stoppingToken);
    }

}