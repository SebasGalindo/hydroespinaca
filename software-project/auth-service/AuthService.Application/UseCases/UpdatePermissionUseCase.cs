using AuthService.Application.DTOs;
using AuthService.Application.Interfaces;
using AuthService.Application.Validators;
using AuthService.Domain.Interfaces;
using AutoMapper;
using FluentValidation;
using HydroEspinaca.Shared.Errors;

namespace AuthService.Application.UseCases;

public class UpdatePermissionUseCase : IUpdatePermissionUseCase
{
    private readonly IPermissionRepository _permissionRepository;
    private readonly UpdatePermissionRequestValidator _validator;
    private readonly IMapper _mapper;

    public UpdatePermissionUseCase(
        IPermissionRepository permissionRepository, 
        UpdatePermissionRequestValidator validator,
        IMapper mapper)
    {
        _permissionRepository = permissionRepository;
        _validator = validator;
        _mapper = mapper;
    }

    public async Task<PermissionResponseDto?> ExecuteAsync(string id, UpdatePermissionRequestDto request)
    {
        var validation = await _validator.ValidateAsync(request);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);

        var permission = await _permissionRepository.FindByIdAsync(id);
        if (permission == null)
            throw new NotFoundException($"No se encontró el permiso con ID '{id}'");

        permission.UpdateDetails(request.Name, request.Description);
        await _permissionRepository.UpdateAsync(permission);

        return _mapper.Map<PermissionResponseDto>(permission);
    }
}