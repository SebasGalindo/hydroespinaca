using HydroEspinaca.Shared.Enums;

namespace SensorService.Domain.Interfaces;

public interface IVariableRepository
{
    Task<Variable?> GetByIdAsync(string id);
    Task<Variable?> GetByCodeAsync(string code);
    Task<List<Variable>> GetAllAsync();
    Task<List<Variable>> GetByRegulationTypeAsync(RegulationType regulationType, CancellationToken cancellationToken = default);
    Task CreateAsync(Variable variable);
    Task UpdateAsync(Variable variable);
    Task DeleteAsync(string id);
    Task<List<string>> GetNonExistingIdsAsync(IEnumerable<string> ids);
    Task<List<string>> GetNonExistingCodesAsync(IEnumerable<string> codes);
}
