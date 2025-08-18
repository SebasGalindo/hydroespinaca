using AuthService.Application.Features.Roles.DTOs;
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

        var role = new Role(request.Code, request.Name, permissionIds);
        await _roleRepository.CreateAsync(role);

        return _mapper.Map<RoleResponseDto>(role);
    }
}