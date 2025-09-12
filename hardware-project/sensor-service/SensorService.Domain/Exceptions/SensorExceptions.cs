using HydroEspinaca.Shared.Errors;

namespace SensorService.Domain.Exceptions;

/// <summary>
/// Exception thrown when a sensor is not found
/// </summary>
public class SensorNotFoundException : NotFoundException
{
    public SensorNotFoundException(string sensorId) 
        : base($"Sensor con el ID {sensorId} no encontrado.")
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
/// Exception thrown when sensor data is not found
/// </summary>
public class SensorDataNotFoundException : NotFoundException
{
    public SensorDataNotFoundException(string message) 
        : base(message)
    {
    }
}

/// <summary>
/// Exception thrown when there's a conflict in sensor operations
/// </summary>
public class SensorOperationConflictException : ConflictException
{
    public SensorOperationConflictException(string message) 
        : base(message)
    {
    }
}