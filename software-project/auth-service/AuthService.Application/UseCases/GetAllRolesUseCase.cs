using AuthService.Application.DTOs;
using AuthService.Application.Interfaces;
using AuthService.Domain.Interfaces;
using AutoMapper;

namespace AuthService.Application.UseCases;

public class GetAllRolesUseCase : IGetAllRolesUseCase
{
    private readonly IRoleRepository _roleRepository;
    private readonly IPermissionRepository _permissionRepository;
    private readonly IMapper _mapper;

    public GetAllRolesUseCase(IRoleRepository roleRepository, IPermissionRepository permissionRepository, IMapper mapper)
    {
        _roleRepository = roleRepository;
        _permissionRepository = permissionRepository;
        _mapper = mapper;
    }

    public async Task<List<RoleResponseDto>> ExecuteAsync()
    {
        var roles = await _roleRepository.GetAllAsync();
        
        // Get all unique permission IDs from all roles
        var allPermissionIds = roles.SelectMany(r => r.Permissions).Distinct().ToList();
        
        // Get all permissions at once for efficiency
        var allPermissions = allPermissionIds.Any() 
            ? await _permissionRepository.FindByIdsAsync(allPermissionIds)
            : new List<Domain.Entities.Permission>();
        
        var permissionIdToCodeMap = allPermissions.ToDictionary(p => p.Id, p => p.Code);
        
        // Map each role and resolve permission codes
        var result = new List<RoleResponseDto>();
        foreach (var role in roles)
        {
            var response = _mapper.Map<RoleResponseDto>(role);
            var permissionCodes = role.Permissions
                .Where(permissionIdToCodeMap.ContainsKey)
                .Select(id => permissionIdToCodeMap[id])
                .ToList();
            
            result.Add(response with { PermissionCodes = permissionCodes });
        }
        
        return result;
    }
}