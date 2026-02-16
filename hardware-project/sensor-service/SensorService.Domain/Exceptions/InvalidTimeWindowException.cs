namespace SensorService.Domain.Exceptions;

/// <summary>
/// Excepción lanzada cuando se intenta crear una ventana temporal inválida
/// (ej: fecha de inicio posterior a la fecha de fin).
/// </summary>
public class InvalidTimeWindowException : DomainException
{
    public InvalidTimeWindowException(string message) : base(message) { }
}

/// <summary>
/// Clase base abstracta para todas las excepciones del dominio del servicio de sensores.
/// </summary>
public abstract class DomainException : Exception
{
    protected DomainException(string message) : base(message) { }
}