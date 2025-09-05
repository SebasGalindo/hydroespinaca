namespace SensorService.Domain.Interfaces;

public interface IVariableMigrationService
{
    Task<int> MigrateVariablesToNewSchemaAsync();
}