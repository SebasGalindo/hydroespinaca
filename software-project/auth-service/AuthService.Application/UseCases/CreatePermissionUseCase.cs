using AuthService.Application.DTOs;
using AuthService.Application.Interfaces;
using AuthService.Application.Validators;
using AuthService.Domain.Entities;
using AuthService.Domain.Interfaces;
using AutoMapper;
using FluentValidation;
using HydroEspinaca.Shared.Errors;

namespace AuthService.Application.UseCases;

public class CreatePermissionUseCase : ICreatePermissionUseCase
{
    private readonly IPermissionRepository _permissionRepository;
    private readonly CreatePermissionRequestValidator _validator;
    private readonly IMapper _mapper;

    public CreatePermissionUseCase(
        IPermissionRepository permissionRepository, 
        CreatePermissionRequestValidator validator,
        IMapper mapper)
    {
        _permissionRepository = permissionRepository;
        _validator = validator;
        _mapper = mapper;
    }

    public async Task<PermissionResponseDto> ExecuteAsync(CreatePermissionRequestDto request)
    {
        var validation = await _validator.ValidateAsync(request);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);

        var permission = _mapper.Map<Permission>(request);
        await _permissionRepository.CreateAsync(permission);

        return _mapper.Map<PermissionResponseDto>(permission);
    }
}