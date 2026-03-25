using HydroEspinaca.Shared.DTOs.Authentication;
using AuthService.Domain.Entities;
using AuthService.Domain.Interfaces;
using AutoMapper;
using MediatR;

namespace AuthService.Application.Features.Permissions.Commands.CreatePermission;

/// <summary>
/// Handler that persists a new permission entity.
/// </summary>
public class CreatePermissionCommandHandler : IRequestHandler<CreatePermissionCommand, PermissionResponseDto>
{
    private readonly IPermissionRepository _permissionRepository;
    private readonly IMapper _mapper;

    public CreatePermissionCommandHandler(IPermissionRepository permissionRepository, IMapper mapper)
    {
        _permissionRepository = permissionRepository;
        _mapper = mapper;
    }

    public async Task<PermissionResponseDto> Handle(CreatePermissionCommand request, CancellationToken cancellationToken)
    {
        var existingPermission = await _permissionRepository.FindByCodeAsync(request.Code);
        if (existingPermission != null)
        {
            throw new ArgumentException($"Ya existe un permiso con el código '{request.Code}'");
        }

        var permission = new Permission(request.Code, request.Name, request.Description);
        await _permissionRepository.CreateAsync(permission);

        return _mapper.Map<PermissionResponseDto>(permission);
    }
}