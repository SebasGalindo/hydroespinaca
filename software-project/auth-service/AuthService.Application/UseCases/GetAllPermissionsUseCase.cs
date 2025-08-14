using AuthService.Application.DTOs;
using AuthService.Application.Interfaces;
using AuthService.Domain.Interfaces;
using AutoMapper;

namespace AuthService.Application.UseCases;

public class GetAllPermissionsUseCase : IGetAllPermissionsUseCase
{
    private readonly IPermissionRepository _permissionRepository;
    private readonly IMapper _mapper;

    public GetAllPermissionsUseCase(IPermissionRepository permissionRepository, IMapper mapper)
    {
        _permissionRepository = permissionRepository;
        _mapper = mapper;
    }

    public async Task<List<PermissionResponseDto>> ExecuteAsync()
    {
        var permissions = await _permissionRepository.GetAllAsync();
        return _mapper.Map<List<PermissionResponseDto>>(permissions);
    }
}