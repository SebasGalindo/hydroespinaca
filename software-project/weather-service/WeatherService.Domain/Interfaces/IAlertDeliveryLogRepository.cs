using WeatherService.Domain.Entities;

namespace WeatherService.Domain.Interfaces;

public interface IAlertDeliveryLogRepository
{
    Task InsertAsync(AlertDeliveryLog log, CancellationToken ct = default);

    Task<bool> ExistsAsync(
        string fuzzySystemId,
        string alertType,
        DateTime forecastDate,
        string userId,
        CancellationToken ct = default);
}
