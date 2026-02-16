namespace ActuatorService.Application.Interfaces;
/// <summary>
/// Contract for validating ESP32 node existence via the sensor service.
/// </summary>
public interface IEsp32ValidationService
{
    Task<bool> ExistsAsync(string esp32Id);
}
