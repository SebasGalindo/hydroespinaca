using HydroEspinaca.Shared.Errors;

namespace AuthService.Domain.Exceptions;
/// <summary>
/// Exception thrown when a token has expired and cannot be used for authentication.
/// </summary>
public class TokenExpiredException : DomainException
{
    public TokenExpiredException() : base("El token ha expirado.") { }
}