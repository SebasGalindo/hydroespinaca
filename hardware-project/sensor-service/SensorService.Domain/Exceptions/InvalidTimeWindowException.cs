namespace SensorService.Domain.Exceptions;

public class InvalidTimeWindowException : DomainException
{
    public InvalidTimeWindowException(string message) : base(message) { }
}

public abstract class DomainException : Exception
{
    protected DomainException(string message) : base(message) { }
}