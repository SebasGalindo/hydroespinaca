using HydroEspinaca.Shared.Errors;

namespace AuthService.Domain.Exceptions;
/// <summary>
/// Exception thrown when a user cannot be found by the specified identifier.
/// </summary>
public class UserNotFoundException : DomainException
{
    public UserNotFoundException(string email) : base($"Usuario no encontrado: {email}") { }
}