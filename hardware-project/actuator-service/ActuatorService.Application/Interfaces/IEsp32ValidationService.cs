namespace ActuatorService.Application.Interfaces;
public interface IEsp32ValidationService
{
    Task<bool> ExistsAsync(string esp32Id);
}
