using HydroEspinaca.Shared.DTOs.Actuator;

namespace ActuatorService.Domain.Interfaces;

public interface IRoutineCommandPublisher
{
    Task PublishJobScheduleAsync(JobScheduleDto jobSchedule);
}