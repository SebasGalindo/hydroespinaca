using AuthService.Application.Interfaces;
using AuthService.Domain.Interfaces;
using HydroEspinaca.Shared.Errors;

namespace AuthService.Application.UseCases;

public class DeletePermissionUseCase : IDeletePermissionUseCase
{
    private readonly IPermissionRepository _permissionRepository;

    public DeletePermissionUseCase(IPermissionRepository permissionRepository)
    {
        _permissionRepository = permissionRepository;
    }

    public async Task<bool> ExecuteAsync(string id)
    {
        var exists = await _permissionRepository.ExistsAsync(id);
        if (!exists)
            throw new NotFoundException($"No se encontró el permiso con ID '{id}'");

        await _permissionRepository.DeleteAsync(id);
        return true;
    }
}