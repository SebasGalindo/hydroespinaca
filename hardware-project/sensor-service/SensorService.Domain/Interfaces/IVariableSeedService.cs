namespace SensorService.Domain.Interfaces;

public interface IVariableSeedService
{
    /// <summary>
    /// Seeds default variables to the database if they don't exist
    /// </summary>
    /// <returns>Number of variables seeded</returns>
    Task<int> SeedDefaultVariablesAsync();
}
