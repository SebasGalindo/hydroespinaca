using AuthService.Application.DTOs;
using AuthService.Application.Validators;
using AuthService.Domain.Interfaces;
using FluentValidation;

namespace AuthService.Application.UseCases;
public class CreateClientAppUseCase
{
    private readonly IClientAppRegistrationService _registrationService;
    private readonly CreateClientAppRequestValidator _validator;

    public CreateClientAppUseCase(
        IClientAppRegistrationService registrationService,
        CreateClientAppRequestValidator validator)
    {
        _registrationService = registrationService;
        _validator = validator;
    }

    public async Task ExecuteAsync(CreateClientAppRequestDto dto)
    {
        var validation = await _validator.ValidateAsync(dto);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);

        await _registrationService.RegisterAsync(
            dto.ClientId,
            dto.Secret,
            dto.Scopes
        );
    }
}