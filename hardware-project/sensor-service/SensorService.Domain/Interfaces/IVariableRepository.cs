namespace SensorService.Domain.Interfaces;

public interface IVariableRepository
{
    Task<Variable?> GetByIdAsync(string id);
    Task<List<Variable>> GetAllAsync();
    Task CreateAsync(Variable variable);
    Task UpdateAsync(Variable variable);
    Task DeleteAsync(string id);
    Task<List<string>> GetNonExistingIdsAsync(IEnumerable<string> ids);
}
