using HydroEspinaca.Shared.DTOs.Authentication;
using AuthService.Domain.Entities;
using AuthService.Domain.Interfaces;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AuthService.Application.Features.Roles.Commands.CreateRole;

public class CreateRoleCommandHandler : IRequestHandler<CreateRoleCommand, RoleResponseDto>
{
    private readonly IRoleRepository _roleRepository;
    private readonly IPermissionRepository _permissionRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<CreateRoleCommandHandler> _logger;

    public CreateRoleCommandHandler(
        IRoleRepository roleRepository,
        IPermissionRepository permissionRepository,
        IMapper mapper,
        ILogger<CreateRoleCommandHandler> logger)
    {
        _roleRepository = roleRepository;
        _permissionRepository = permissionRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<RoleResponseDto> Handle(CreateRoleCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating role with Code: {RoleCode}, Name: {RoleName}, PermissionCount: {PermissionCount}",
            request.Code, request.Name, request.PermissionCodes?.Count ?? 0);

        var existingRole = await _roleRepository.FindByCodeAsync(request.Code);
        if (existingRole != null)
        {
            _logger.LogWarning("Cannot create role: Code {RoleCode} already exists", request.Code);
            throw new ArgumentException($"Ya existe un rol con el código '{request.Code}'");
        }

        // Validate provided permission codes exist
        _logger.LogDebug("Validating {PermissionCount} permission codes: {PermissionCodes}",
            request.PermissionCodes.Count, string.Join(", ", request.PermissionCodes));

        var nonExisting = await _permissionRepository.GetNonExistingCodesAsync(request.PermissionCodes);
        if (nonExisting.Count > 0)
        {
            _logger.LogWarning("Cannot create role {RoleCode}: {NonExistingCount} permission codes not found: {NonExistingCodes}",
                request.Code, nonExisting.Count, string.Join(", ", nonExisting));
            throw new ArgumentException($"No se encontraron los permisos con códigos: {string.Join(", ", nonExisting)}");
        }

        var distinctCodes = request.PermissionCodes.Distinct().ToList();
        var role = new Role(request.Code, request.Name, distinctCodes);
        await _roleRepository.CreateAsync(role);

        _logger.LogInformation("Successfully created role {RoleCode} with {PermissionCount} permissions", request.Code, distinctCodes.Count);
        return _mapper.Map<RoleResponseDto>(role);
    }
}