using AuthService.Application.DTOs;
using AuthService.Application.Interfaces;
using AuthService.Application.Validators;
using AuthService.Domain.Entities;
using AuthService.Domain.Interfaces;
using AutoMapper;
using FluentValidation;
using HydroEspinaca.Shared.Errors;

namespace AuthService.Application.UseCases;

public class CreateRoleUseCase : ICreateRoleUseCase
{
    private readonly IRoleRepository _roleRepository;
    private readonly IPermissionRepository _permissionRepository;
    private readonly CreateRoleRequestValidator _validator;
    private readonly IMapper _mapper;

    public CreateRoleUseCase(
        IRoleRepository roleRepository, 
        IPermissionRepository permissionRepository,
        CreateRoleRequestValidator validator,
        IMapper mapper)
    {
        _roleRepository = roleRepository;
        _permissionRepository = permissionRepository;
        _validator = validator;
        _mapper = mapper;
    }

    public async Task<RoleResponseDto> ExecuteAsync(CreateRoleRequestDto request)
    {
        var validation = await _validator.ValidateAsync(request);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);

        // Resolve permission codes to ObjectIds
        List<string> permissionIds = new();
        if (request.PermissionCodes.Any())
        {
            var permissions = await _permissionRepository.FindByCodesAsync(request.PermissionCodes);
            permissionIds = permissions.Select(p => p.Id).ToList();
        }

        var role = _mapper.Map<Role>(request);
        role.SetPermissions(permissionIds);
        
        await _roleRepository.CreateAsync(role);

        // Map to response DTO with permission codes
        var response = _mapper.Map<RoleResponseDto>(role);
        response = response with { PermissionCodes = request.PermissionCodes };

        return response;
    }
}