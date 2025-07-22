using ActuatorService.Application.DTOs;

namespace ActuatorService.Application.Interfaces;

public interface ICommandService
{
    Task RegisterCommandAsync(CreateCommandDto dto, string? userId);
}
