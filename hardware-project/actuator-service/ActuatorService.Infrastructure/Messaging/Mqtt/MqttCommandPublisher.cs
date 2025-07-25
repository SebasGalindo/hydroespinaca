using ActuatorService.Application.UseCases.Commands;
using ActuatorService.Domain.Entities;
using ActuatorService.Domain.Interfaces;
using HydroEspinaca.Shared.Mqtt;

public class MqttCommandPublisher : ICommandPublisher
{
    private readonly IMqttClientService _mqttClient;

    public MqttCommandPublisher(IMqttClientService mqttClient)
    {
        _mqttClient = mqttClient;
    }

    public async Task PublishAsync(ActuatorCommand command)
    {
        var (topic, payload) = CommandMessageBuilder.Build(command);
        await _mqttClient.PublishAsync(topic, payload);
    }
}
