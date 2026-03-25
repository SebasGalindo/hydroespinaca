using HydroEspinaca.Shared.Errors;

namespace SensorService.Domain.Exceptions;

/// <summary>
/// Excepción lanzada cuando no se encuentra un sensor con el identificador especificado.
/// </summary>
public class SensorNotFoundException : NotFoundException
{
    /// <summary>
    /// Crea una nueva instancia de la excepción para un sensor no encontrado.
    /// </summary>
    /// <param name="sensorId">Identificador del sensor no encontrado.</param>
    public SensorNotFoundException(string sensorId) 
        : base($"Sensor con el ID {sensorId} no encontrado.")
    {
    }
}

/// <summary>
/// Excepción lanzada cuando no se encuentra un nodo ESP32 con el identificador especificado.
/// </summary>
public class Esp32NotFoundException : NotFoundException
{
    /// <summary>
    /// Crea una nueva instancia de la excepción para un nodo ESP32 no encontrado.
    /// </summary>
    /// <param name="esp32Id">Identificador del nodo ESP32 no encontrado.</param>
    public Esp32NotFoundException(string esp32Id) 
        : base($"ESP32 con el ID {esp32Id} no encontrado.")
    {
    }
}

/// <summary>
/// Excepción lanzada cuando no se encuentran datos de sensores para la consulta solicitada.
/// </summary>
public class SensorDataNotFoundException : NotFoundException
{
    /// <summary>
    /// Crea una nueva instancia de la excepción con un mensaje personalizado.
    /// </summary>
    /// <param name="message">Mensaje descriptivo del error.</param>
    public SensorDataNotFoundException(string message) 
        : base(message)
    {
    }
}

/// <summary>
/// Excepción lanzada cuando existe un conflicto en una operación sobre sensores
/// (ej: código duplicado, sensor ya registrado).
/// </summary>
public class SensorOperationConflictException : ConflictException
{
    /// <summary>
    /// Crea una nueva instancia de la excepción de conflicto con un mensaje personalizado.
    /// </summary>
    /// <param name="message">Mensaje descriptivo del conflicto.</param>
    public SensorOperationConflictException(string message) 
        : base(message)
    {
    }
}