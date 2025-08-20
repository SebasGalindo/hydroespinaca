using AuthService.Domain.Interfaces;
using HydroEspinaca.Shared.Utils;
using MediatR;

namespace AuthService.Application.Features.Roles.Commands.DeleteRole;

public class DeleteRoleCommandHandler : IRequestHandler<DeleteRoleCommand, bool>
{
    private readonly IRoleRepository _roleRepository;

    public DeleteRoleCommandHandler(IRoleRepository roleRepository)
    {
        _roleRepository = roleRepository;
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

        await _roleRepository.DeleteAsync(role.Id);
        return true;
    }
}