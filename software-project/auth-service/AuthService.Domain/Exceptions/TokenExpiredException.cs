using HydroEspinaca.Shared.Errors;

namespace AuthService.Domain.Exceptions;
public class TokenExpiredException : DomainException
{
    public TokenExpiredException() : base("El token ha expirado.") { }
}