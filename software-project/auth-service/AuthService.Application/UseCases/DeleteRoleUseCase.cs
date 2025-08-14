using AuthService.Application.Interfaces;
using AuthService.Domain.Interfaces;
using HydroEspinaca.Shared.Errors;

namespace AuthService.Application.UseCases;

public class DeleteRoleUseCase : IDeleteRoleUseCase
{
    private readonly IRoleRepository _roleRepository;

    public DeleteRoleUseCase(IRoleRepository roleRepository)
    {
        _roleRepository = roleRepository;
    }

    public async Task<bool> ExecuteAsync(string id)
    {
        var exists = await _roleRepository.ExistsAsync(id);
        if (!exists)
            throw new NotFoundException($"No se encontró el rol con ID '{id}'");

        await _roleRepository.DeleteAsync(id);
        return true;
    }
}