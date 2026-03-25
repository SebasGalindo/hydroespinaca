using HydroEspinaca.Shared.Errors;

namespace AuthService.Domain.Exceptions;

/// <summary>
/// Exception thrown when attempting to delete a role that still has users assigned to it.
/// </summary>
public class RoleHasAssignedUsersException : DomainException
{
    public RoleHasAssignedUsersException(string roleId, long userCount)
        : base($"No se puede eliminar el rol '{roleId}' porque tiene {userCount} usuario(s) asignado(s)")
    { }
}
