namespace HydroEspinaca.Shared.Errors;
public class UnauthorizedException : Exception
{
    public UnauthorizedException(string message) : base(message) { }
}