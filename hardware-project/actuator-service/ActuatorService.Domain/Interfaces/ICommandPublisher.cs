using ActuatorService.Domain.Entities;

namespace ActuatorService.Domain.Interfaces;

public interface ICommandPublisher
{
    Task PublishAsync(ActuatorCommand command);
}
