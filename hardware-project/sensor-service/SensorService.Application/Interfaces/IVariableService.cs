using HydroEspinaca.Shared.DTOs.Variables;

namespace SensorService.Application.Interfaces;

public interface IVariableService
{
    Task<List<VariableDto>> GetAllAsync();
    Task<VariableDto?> GetByIdAsync(string id);
    Task AddAsync(VariableCreateDto dto);
    Task UpdateAsync(VariableUpdateDto dto);
    Task DeleteAsync(string id);
}
