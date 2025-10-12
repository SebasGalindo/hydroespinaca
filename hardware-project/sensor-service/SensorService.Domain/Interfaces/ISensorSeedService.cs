namespace SensorService.Domain.Interfaces;

public interface ISensorSeedService
{
    /// <summary>
    /// Seeds default sensors to the database if they don't exist
    /// </summary>
    /// <returns>Number of sensors seeded</returns>
    Task<int> SeedDefaultSensorsAsync();
}
