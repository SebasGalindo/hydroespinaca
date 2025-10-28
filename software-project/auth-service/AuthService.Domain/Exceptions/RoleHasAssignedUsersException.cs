using HydroEspinaca.Shared.Errors;

namespace AuthService.Domain.Exceptions;

public class RoleHasAssignedUsersException : DomainException
{
    public RoleHasAssignedUsersException(string roleId, long userCount)
        : base($"No se puede eliminar el rol '{roleId}' porque tiene {userCount} usuario(s) asignado(s)")
    { }
}
