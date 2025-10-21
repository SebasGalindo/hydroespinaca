using AuthService.Domain.Interfaces;
using AutoMapper;
using HydroEspinaca.Shared.DTOs.Authentication;
using MediatR;

namespace AuthService.Application.Features.Permissions.Queries.GetGroupedPermissions;

public class GetGroupedPermissionsQueryHandler : IRequestHandler<GetGroupedPermissionsQuery, List<GroupedPermissionResponseDto>>
{
    private readonly IPermissionRepository _permissionRepository;
    private readonly IMapper _mapper;

    public GetGroupedPermissionsQueryHandler(IPermissionRepository permissionRepository, IMapper mapper)
    {
        _permissionRepository = permissionRepository;
        _mapper = mapper;
    }

    public async Task<List<GroupedPermissionResponseDto>> Handle(GetGroupedPermissionsQuery request, CancellationToken cancellationToken)
    {
        var grouped = await _permissionRepository.GetGroupedAsync();

        // If repository returns domain projections, map accordingly; otherwise map manually
        var result = new List<GroupedPermissionResponseDto>(grouped.Count);
        foreach (var group in grouped)
        {
            result.Add(new GroupedPermissionResponseDto
            {
                Category = group.Category,
                Permissions = group.Permissions
                    .OrderBy(p => p.Code)
                    .Select(p => new PermissionResponseDto
                {
                    Id = p.Id,
                    Code = p.Code,
                    Name = p.Name
                }).ToList()
            });
        }

        return result;
    }
}
