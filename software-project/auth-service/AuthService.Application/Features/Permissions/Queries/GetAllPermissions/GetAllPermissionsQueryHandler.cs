using HydroEspinaca.Shared.DTOs.Authentication;
using AuthService.Domain.Interfaces;
using AutoMapper;
using MediatR;

namespace AuthService.Application.Features.Permissions.Queries.GetAllPermissions;

/// <summary>
/// Handler that returns all registered permissions.
/// </summary>
public class GetAllPermissionsQueryHandler : IRequestHandler<GetAllPermissionsQuery, List<PermissionResponseDto>>
{
    private readonly IPermissionRepository _permissionRepository;
    private readonly IMapper _mapper;

    public GetAllPermissionsQueryHandler(IPermissionRepository permissionRepository, IMapper mapper)
    {
        _permissionRepository = permissionRepository;
        _mapper = mapper;
    }

    public async Task<List<PermissionResponseDto>> Handle(GetAllPermissionsQuery request, CancellationToken cancellationToken)
    {
        var permissions = await _permissionRepository.GetAllAsync();
        return _mapper.Map<List<PermissionResponseDto>>(permissions);
    }
}