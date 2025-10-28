using AuthService.Domain.Exceptions;
using AuthService.Domain.Interfaces;
using HydroEspinaca.Shared.Utils;
using MediatR;

namespace AuthService.Application.Features.Roles.Commands.DeleteRole;

public class DeleteRoleCommandHandler : IRequestHandler<DeleteRoleCommand, bool>
{
    private readonly IRoleRepository _roleRepository;
    private readonly IUserRepository _userRepository;

    public DeleteRoleCommandHandler(IRoleRepository roleRepository, IUserRepository userRepository)
    {
        _roleRepository = roleRepository;
        _userRepository = userRepository;
    }

    public async Task<bool> Handle(DeleteRoleCommand request, CancellationToken cancellationToken)
    {
        var role = await _roleRepository.FindByCodeAsync(request.IdOrCode);
        if (role == null && ObjectIdHelper.IsValidObjectId(request.IdOrCode))
        {
            role = await _roleRepository.FindByIdAsync(request.IdOrCode);
        }

        if (role == null)
        {
            return false;
        }

        // Verificar si hay usuarios asignados a este rol
        var userCount = await _userRepository.CountByRoleIdAsync(role.Id);
        if (userCount > 0)
        {
            throw new RoleHasAssignedUsersException(role.Code, userCount);
        }

        await _roleRepository.DeleteAsync(role.Id);
        return true;
    }
}