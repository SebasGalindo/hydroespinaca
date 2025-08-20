using AuthService.Domain.Interfaces;
using HydroEspinaca.Shared.Utils;
using MediatR;

namespace AuthService.Application.Features.Permissions.Commands.DeletePermission;

public class DeletePermissionCommandHandler : IRequestHandler<DeletePermissionCommand, bool>
{
    private readonly IPermissionRepository _permissionRepository;

    public DeletePermissionCommandHandler(IPermissionRepository permissionRepository)
    {
        _permissionRepository = permissionRepository;
    }

    public async Task<bool> Handle(DeletePermissionCommand request, CancellationToken cancellationToken)
    {
        var permission = await _permissionRepository.FindByCodeAsync(request.IdOrCode);
        if (permission == null && ObjectIdHelper.IsValidObjectId(request.IdOrCode))
        {
            permission = await _permissionRepository.FindByIdAsync(request.IdOrCode);
        }
        
        if (permission == null)
        {
            return false;
        }

        await _permissionRepository.DeleteAsync(permission.Id);
        return true;
    }
}