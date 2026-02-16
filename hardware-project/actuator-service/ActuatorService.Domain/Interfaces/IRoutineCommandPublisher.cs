using HydroEspinaca.Shared.DTOs.Actuator;

namespace ActuatorService.Domain.Interfaces;

/// <summary>
/// Contract for publishing actuator control commands via MQTT to ESP32 nodes.
/// </summary>
public interface IRoutineCommandPublisher
{
    Task PublishJobScheduleAsync(JobScheduleDto jobSchedule);
}