using AuthService.Application.DTOs;
using AuthService.Application.Interfaces;
using AuthService.Application.Validators;
using AuthService.Domain.Interfaces;
using AutoMapper;
using FluentValidation;
using HydroEspinaca.Shared.Errors;

namespace AuthService.Application.UseCases;

public class UpdateRoleUseCase : IUpdateRoleUseCase
{
    private readonly IRoleRepository _roleRepository;
    private readonly IPermissionRepository _permissionRepository;
    private readonly UpdateRoleRequestValidator _validator;
    private readonly IMapper _mapper;

    public UpdateRoleUseCase(
        IRoleRepository roleRepository,
        IPermissionRepository permissionRepository,
        UpdateRoleRequestValidator validator,
        IMapper mapper)
    {
        _roleRepository = roleRepository;
        _permissionRepository = permissionRepository;
        _validator = validator;
        _mapper = mapper;
    }

    public async Task<RoleResponseDto?> ExecuteAsync(string idOrCode, UpdateRoleRequestDto request)
    {
        var validation = await _validator.ValidateAsync(request);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);

        // Try to find by Code first (for API), then by Id
        var role = await _roleRepository.FindByCodeAsync(idOrCode) ??
                  await _roleRepository.FindByIdAsync(idOrCode);
        
        if (role == null)
            throw new NotFoundException($"No se encontró el rol con identificador '{idOrCode}'");

        // Resolve permission codes to ObjectIds
        List<string> permissionIds = new();
        if (request.PermissionCodes.Any())
        {
            var permissions = await _permissionRepository.FindByCodesAsync(request.PermissionCodes);
            permissionIds = permissions.Select(p => p.Id).ToList();
        }

        role.UpdateName(request.Name);
        role.SetPermissions(permissionIds);
        await _roleRepository.UpdateAsync(role);

        // Map to response DTO with permission codes
        var response = _mapper.Map<RoleResponseDto>(role);
        response = response with { PermissionCodes = request.PermissionCodes };

        return response;
    }
}