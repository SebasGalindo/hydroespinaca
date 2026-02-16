using HydroEspinaca.Shared.DTOs.Authentication;
using AuthService.Domain.Interfaces;
using AutoMapper;
using HydroEspinaca.Shared.Utils;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AuthService.Application.Features.Roles.Commands.UpdateRole;

/// <summary>
/// Handler that updates a role entity and its permission associations.
/// </summary>
public class UpdateRoleCommandHandler : IRequestHandler<UpdateRoleCommand, RoleResponseDto?>
{
    private readonly IRoleRepository _roleRepository;
    private readonly IPermissionRepository _permissionRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<UpdateRoleCommandHandler> _logger;

    public UpdateRoleCommandHandler(
        IRoleRepository roleRepository,
        IPermissionRepository permissionRepository,
        IMapper mapper,
        ILogger<UpdateRoleCommandHandler> logger)
    {
        _roleRepository = roleRepository;
        _permissionRepository = permissionRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<RoleResponseDto?> Handle(UpdateRoleCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Updating role with IdOrCode: {IdOrCode}, Name: {Name}, PermissionCount: {PermissionCount}",
            request.IdOrCode, request.Name, request.PermissionCodes?.Count ?? 0);

        var role = await _roleRepository.FindByCodeAsync(request.IdOrCode);
        if (role == null && ObjectIdHelper.IsValidObjectId(request.IdOrCode))
        {
            _logger.LogDebug("Role not found by code, trying by ObjectId: {IdOrCode}", request.IdOrCode);
            role = await _roleRepository.FindByIdAsync(request.IdOrCode);
        }

        if (role == null)
        {
            _logger.LogWarning("Role not found with IdOrCode: {IdOrCode}", request.IdOrCode);
            return null;
        }

        // Validate provided codes exist
        _logger.LogDebug("Validating {PermissionCount} permission codes: {PermissionCodes}",
            request.PermissionCodes.Count, string.Join(", ", request.PermissionCodes));

        var nonExisting = await _permissionRepository.GetNonExistingCodesAsync(request.PermissionCodes);
        if (nonExisting.Count > 0)
        {
            _logger.LogWarning("Cannot update role {IdOrCode}: {NonExistingCount} permission codes not found: {NonExistingCodes}",
                request.IdOrCode, nonExisting.Count, string.Join(", ", nonExisting));
            throw new ArgumentException($"No se encontraron los permisos con códigos: {string.Join(", ", nonExisting)}");
        }

        role.UpdateName(request.Name);
        role.SetPermissions(request.PermissionCodes.Distinct().ToList());
        await _roleRepository.UpdateAsync(role);

        _logger.LogInformation("Successfully updated role {RoleCode} with {PermissionCount} permissions", role.Code, request.PermissionCodes.Count);
        return _mapper.Map<RoleResponseDto>(role);
    }
}