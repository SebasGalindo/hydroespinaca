using HydroEspinaca.Shared.Errors;

namespace AuthService.Domain.Exceptions;
public class UserNotFoundException : DomainException
{
    public UserNotFoundException(string email) : base($"Usuario no encontrado: {email}") { }
}