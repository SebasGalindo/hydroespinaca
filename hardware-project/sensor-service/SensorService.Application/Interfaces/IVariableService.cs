using SensorService.Application.DTOs;

namespace SensorService.Application.Interfaces;

public interface IVariableService
{
    Task<List<VariableDto>> GetAllAsync();
    Task<VariableDto?> GetByIdAsync(string id);
    Task AddAsync(VariableDto dto);
    Task UpdateAsync(VariableDto dto);
    Task DeleteAsync(string id);
}