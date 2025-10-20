using HydroEspinaca.Shared.DTOs.Authentication;
using AuthService.Domain.Entities;
using AuthService.Domain.Interfaces;
using AutoMapper;
using MediatR;

namespace AuthService.Application.Features.Roles.Commands.CreateRole;

public class CreateRoleCommandHandler : IRequestHandler<CreateRoleCommand, RoleResponseDto>
{
    private readonly IRoleRepository _roleRepository;
    private readonly IPermissionRepository _permissionRepository;
    private readonly IMapper _mapper;

    public CreateRoleCommandHandler(
        IRoleRepository roleRepository,
        IPermissionRepository permissionRepository,
        IMapper mapper)
    {
        _roleRepository = roleRepository;
        _permissionRepository = permissionRepository;
        _mapper = mapper;
    }

    public async Task<RoleResponseDto> Handle(CreateRoleCommand request, CancellationToken cancellationToken)
    {
        var existingRole = await _roleRepository.FindByCodeAsync(request.Code);
        if (existingRole != null)
        {
            throw new ArgumentException($"Ya existe un rol con el código '{request.Code}'");
        }

        // Validate provided permission codes exist
        var nonExisting = await _permissionRepository.GetNonExistingCodesAsync(request.PermissionCodes);
        if (nonExisting.Count > 0)
        {
            throw new ArgumentException($"No se encontraron los permisos con códigos: {string.Join(", ", nonExisting)}");
        }

        var distinctCodes = request.PermissionCodes.Distinct().ToList();
        var role = new Role(request.Code, request.Name, distinctCodes);
        await _roleRepository.CreateAsync(role);

        return _mapper.Map<RoleResponseDto>(role);
    }
}