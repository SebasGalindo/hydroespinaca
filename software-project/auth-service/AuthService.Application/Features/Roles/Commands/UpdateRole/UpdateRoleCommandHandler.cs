using AuthService.Application.Features.Roles.DTOs;
using AuthService.Domain.Interfaces;
using AutoMapper;
using HydroEspinaca.Shared.Utils;
using MediatR;

namespace AuthService.Application.Features.Roles.Commands.UpdateRole;

public class UpdateRoleCommandHandler : IRequestHandler<UpdateRoleCommand, RoleResponseDto?>
{
    private readonly IRoleRepository _roleRepository;
    private readonly IPermissionRepository _permissionRepository;
    private readonly IMapper _mapper;

    public UpdateRoleCommandHandler(
        IRoleRepository roleRepository,
        IPermissionRepository permissionRepository,
        IMapper mapper)
    {
        _roleRepository = roleRepository;
        _permissionRepository = permissionRepository;
        _mapper = mapper;
    }

    public async Task<RoleResponseDto?> Handle(UpdateRoleCommand request, CancellationToken cancellationToken)
    {
        var role = await _roleRepository.FindByCodeAsync(request.IdOrCode);
        if (role == null && ObjectIdHelper.IsValidObjectId(request.IdOrCode))
        {
            role = await _roleRepository.FindByIdAsync(request.IdOrCode);
        }
        
        if (role == null)
        {
            return null;
        }

        var permissionIds = new List<string>();
        foreach (var permissionCode in request.PermissionCodes)
        {
            var permission = await _permissionRepository.FindByCodeAsync(permissionCode);
            if (permission == null)
            {
                throw new ArgumentException($"No se encontró el permiso con código '{permissionCode}'");
            }
            permissionIds.Add(permission.Id);
        }

        role.UpdateName(request.Name);
        role.SetPermissions(permissionIds);
        await _roleRepository.UpdateAsync(role);

        return _mapper.Map<RoleResponseDto>(role);
    }
}