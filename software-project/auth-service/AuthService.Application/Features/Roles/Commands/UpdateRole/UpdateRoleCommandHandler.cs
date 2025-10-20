using HydroEspinaca.Shared.DTOs.Authentication;
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

        // Validate provided codes exist
        var nonExisting = await _permissionRepository.GetNonExistingCodesAsync(request.PermissionCodes);
        if (nonExisting.Count > 0)
        {
            throw new ArgumentException($"No se encontraron los permisos con códigos: {string.Join(", ", nonExisting)}");
        }

        role.UpdateName(request.Name);
        role.SetPermissions(request.PermissionCodes.Distinct().ToList());
        await _roleRepository.UpdateAsync(role);

        return _mapper.Map<RoleResponseDto>(role);
    }
}