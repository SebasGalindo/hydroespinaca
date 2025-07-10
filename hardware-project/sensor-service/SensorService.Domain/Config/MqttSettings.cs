namespace SensorService.Domain.Config;

public class MqttSettings
{
    public string Host { get; set; } = default!;
    public int Port { get; set; }
    public string ClientId { get; set; } = default!;
    public string Username { get; set; } = default!;
    public string Password { get; set; } = default!;
    public string Topic { get; set; } = "sensor/#";
}
