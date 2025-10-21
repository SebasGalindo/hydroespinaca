using HydroEspinaca.Shared.DTOs.Authentication;
using AuthService.Domain.Interfaces;
using AutoMapper;
using HydroEspinaca.Shared.Utils;
using MediatR;

namespace AuthService.Application.Features.Roles.Queries.GetRole;

public class GetRoleQueryHandler : IRequestHandler<GetRoleQuery, RoleResponseDto?>
{
    private readonly IRoleRepository _roleRepository;
    private readonly IMapper _mapper;

    public GetRoleQueryHandler(IRoleRepository roleRepository, IMapper mapper)
    {
        _roleRepository = roleRepository;
        _mapper = mapper;
    }

    public async Task<RoleResponseDto?> Handle(GetRoleQuery request, CancellationToken cancellationToken)
    {
        var role = await _roleRepository.FindByCodeAsync(request.IdOrCode);
        if (role == null && ObjectIdHelper.IsValidObjectId(request.IdOrCode))
        {
            role = await _roleRepository.FindByIdAsync(request.IdOrCode);
        }
        
        return role == null ? null : _mapper.Map<RoleResponseDto>(role);
    }
}