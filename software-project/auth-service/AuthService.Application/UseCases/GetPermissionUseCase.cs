using AuthService.Application.DTOs;
using AuthService.Application.Interfaces;
using AuthService.Domain.Interfaces;
using AutoMapper;

namespace AuthService.Application.UseCases;

public class GetPermissionUseCase : IGetPermissionUseCase
{
    private readonly IPermissionRepository _permissionRepository;
    private readonly IMapper _mapper;

    public GetPermissionUseCase(IPermissionRepository permissionRepository, IMapper mapper)
    {
        _permissionRepository = permissionRepository;
        _mapper = mapper;
    }

    public async Task<PermissionResponseDto?> ExecuteAsync(string idOrCode)
    {
        // Try to find by Code first (for API), then by Id
        var permission = await _permissionRepository.FindByCodeAsync(idOrCode) ??
                        await _permissionRepository.FindByIdAsync(idOrCode);
        
        if (permission == null)
            return null;

        return _mapper.Map<PermissionResponseDto>(permission);
    }
}