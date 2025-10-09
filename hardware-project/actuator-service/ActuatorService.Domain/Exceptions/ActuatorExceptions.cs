using HydroEspinaca.Shared.Errors;

namespace ActuatorService.Domain.Exceptions;

/// <summary>
/// Exception thrown when an actuator is not found
/// </summary>
public class ActuatorNotFoundException : NotFoundException
{
    public ActuatorNotFoundException(string actuatorId) 
        : base($"Actuator con el ID {actuatorId} no encontrado.")
    {
    }
}

/// <summary>
/// Exception thrown when an ESP32 is not found
/// </summary>
public class Esp32NotFoundException : NotFoundException
{
    public Esp32NotFoundException(string esp32Id) 
        : base($"ESP32 con el ID {esp32Id} no encontrado.")
    {
    }
}

/// <summary>
/// Exception thrown when a routine is not found
/// </summary>
public class RoutineNotFoundException : NotFoundException
{
    public RoutineNotFoundException(string routineId) 
        : base($"Routine con el ID {routineId} no encontrada.")
    {
    }
}

/// <summary>
/// Exception thrown when a scheduled job is not found
/// </summary>
public class ScheduledJobNotFoundException : NotFoundException
{
    public ScheduledJobNotFoundException(string jobId) 
        : base($"Scheduled job con el ID {jobId} no encontrado.")
    {
    }
}

/// <summary>
/// Exception thrown when there's a conflict in routine scheduling
/// </summary>
public class RoutineScheduleConflictException : ConflictException
{
    public RoutineScheduleConflictException(string message)
        : base(message)
    {
    }
}

/// <summary>
/// Exception thrown when a control output is not found
/// </summary>
public class ControlOutputNotFoundException : NotFoundException
{
    public ControlOutputNotFoundException(string controlOutputId)
        : base($"Control output con el ID {controlOutputId} no encontrado.")
    {
    }
}