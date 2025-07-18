using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using MQTTnet;
using SensorService.Application.DTOs.Mqtt;
using SensorService.Application.Interfaces.UseCases.ProcessReadingBatch;
using SensorService.Domain.Config;
using System.Buffers;
using System.Text;
using System.Text.Json;
public class MqttClientService : BackgroundService
{
    private readonly IMqttClient _client;
    private readonly MqttClientOptions _options;
    private readonly IServiceProvider _serviceProvider;

    public MqttClientService(
        IServiceProvider serviceProvider,
        IOptions<MqttSettings> mqttOptions)
    {
        _serviceProvider = serviceProvider;

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
                // Deserialize message
                var dto = JsonSerializer.Deserialize<ReadingBatchDto>(payload, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                // Process through use case
                using var scope = _serviceProvider.CreateScope();
                var useCase = scope.ServiceProvider.GetRequiredService<IProcessReadingBatchUseCase>();

               if (dto is null)
                {
                    Console.WriteLine("❌ Error parsing JSON: DTO is null");
                    return;
                }

                var result = await useCase.ExecuteAsync(dto, stoppingToken);

                if (result.IsSuccess && result.Value is not null)
                {
                    Console.WriteLine($"✅ Lecturas procesadas: {result.Value.TotalReadings}, Alertas generadas: {result.Value.TotalAlerts}");
                }
                else
                {
                    Console.WriteLine($"❌ Error procesando batch: {result.Error}");
                }

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