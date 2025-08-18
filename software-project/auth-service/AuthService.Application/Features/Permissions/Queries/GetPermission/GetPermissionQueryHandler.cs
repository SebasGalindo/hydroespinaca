using AuthService.Application.Features.Permissions.DTOs;
using AuthService.Domain.Interfaces;
using AutoMapper;
using HydroEspinaca.Shared.Utils;
using MediatR;

namespace AuthService.Application.Features.Permissions.Queries.GetPermission;

public class GetPermissionQueryHandler : IRequestHandler<GetPermissionQuery, PermissionResponseDto?>
{
    private readonly IPermissionRepository _permissionRepository;
    private readonly IMapper _mapper;

    public GetPermissionQueryHandler(IPermissionRepository permissionRepository, IMapper mapper)
    {
        _permissionRepository = permissionRepository;
        _mapper = mapper;
    }

    public async Task<PermissionResponseDto?> Handle(GetPermissionQuery request, CancellationToken cancellationToken)
    {
        var permission = await _permissionRepository.FindByCodeAsync(request.IdOrCode);
        if (permission == null && ObjectIdHelper.IsValidObjectId(request.IdOrCode))
        {
            permission = await _permissionRepository.FindByIdAsync(request.IdOrCode);
        }
        
        return permission == null ? null : _mapper.Map<PermissionResponseDto>(permission);
    }
}