using HydroEspinaca.Shared.DTOs.Authentication;
using AuthService.Domain.Interfaces;
using AutoMapper;
using HydroEspinaca.Shared.Utils;
using MediatR;

namespace AuthService.Application.Features.Permissions.Commands.UpdatePermission;

/// <summary>
/// Handler that updates a permission entity.
/// </summary>
public class UpdatePermissionCommandHandler : IRequestHandler<UpdatePermissionCommand, PermissionResponseDto?>
{
    private readonly IPermissionRepository _permissionRepository;
    private readonly IMapper _mapper;

    public UpdatePermissionCommandHandler(IPermissionRepository permissionRepository, IMapper mapper)
    {
        _permissionRepository = permissionRepository;
        _mapper = mapper;
    }

    public async Task<PermissionResponseDto?> Handle(UpdatePermissionCommand request, CancellationToken cancellationToken)
    {
        var permission = await _permissionRepository.FindByCodeAsync(request.IdOrCode);
        if (permission == null && ObjectIdHelper.IsValidObjectId(request.IdOrCode))
        {
            permission = await _permissionRepository.FindByIdAsync(request.IdOrCode);
        }
        
        if (permission == null)
        {
            return null;
        }

        permission.UpdateDetails(request.Name, request.Description);
        await _permissionRepository.UpdateAsync(permission);

        return _mapper.Map<PermissionResponseDto>(permission);
    }
}