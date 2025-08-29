using HydroEspinaca.Shared.DTOs.Actuator;

namespace ActuatorService.Application.Interfaces;

public interface IExecuteMultiRoutineCommandUseCase
{
    Task<List<string>> ExecuteAsync(MultiRoutineCommandDto multiRoutineCommand);
}