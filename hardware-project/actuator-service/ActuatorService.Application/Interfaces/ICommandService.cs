using HydroEspinaca.Shared.DTOs.Actuator;

namespace ActuatorService.Application.Interfaces;

public interface ICommandService
{
    Task AddAsync(CreateCommandDto dto, string? userId);
    Task<List<ActuatorCommandDto>> GetByActuatorIdAsync(string actuatorId);
    Task<List<ActuatorCommandDto>> GetByDateRangeAsync(DateTime from, DateTime to);
}
