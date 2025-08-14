using AuthService.Application.DTOs;
using AuthService.Application.Interfaces;
using AuthService.Domain.Interfaces;
using AutoMapper;

namespace AuthService.Application.UseCases;

public class GetRoleUseCase : IGetRoleUseCase
{
    private readonly IRoleRepository _roleRepository;
    private readonly IPermissionRepository _permissionRepository;
    private readonly IMapper _mapper;

    public GetRoleUseCase(IRoleRepository roleRepository, IPermissionRepository permissionRepository, IMapper mapper)
    {
        _roleRepository = roleRepository;
        _permissionRepository = permissionRepository;
        _mapper = mapper;
    }

    public async Task<RoleResponseDto?> ExecuteAsync(string idOrCode)
    {
        // Try to find by Code first (for API), then by Id
        var role = await _roleRepository.FindByCodeAsync(idOrCode) ??
                  await _roleRepository.FindByIdAsync(idOrCode);
        
        if (role == null)
            return null;

        // Resolve permission ObjectIds to codes for response
        List<string> permissionCodes = new();
        if (role.Permissions.Any())
        {
            var permissions = await _permissionRepository.FindByIdsAsync(role.Permissions);
            permissionCodes = permissions.Select(p => p.Code).ToList();
        }

        var response = _mapper.Map<RoleResponseDto>(role);
        response = response with { PermissionCodes = permissionCodes };

        return response;
    }
}