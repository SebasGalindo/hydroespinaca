using AuthService.Application.Exceptions;
using AuthService.Domain.Entities;
using AuthService.Domain.Interfaces;
using HydroEspinaca.Shared.Errors;
using Microsoft.AspNetCore.DataProtection;

namespace AuthService.Infrastructure.Services;
public class ClientAuthenticationService : IClientAuthenticationService
{
    private readonly IClientAppRepository _appRepo;
    private readonly IPasswordHasher _hasher;

    public ClientAuthenticationService(
        IClientAppRepository appRepo,
        IPasswordHasher hasher)
    {
        _appRepo = appRepo;
        _hasher = hasher;
    }

    public async Task<ClientApp> AuthenticateClientAsync(string clientId, string clientSecret)
    {
        var app = await _appRepo.FindByClientIdAsync(clientId)
                  ?? throw new InvalidClientCredentialsException();

        if (!app.VerifySecret(clientSecret, _hasher))
            throw new InvalidClientCredentialsException();

        return app;
    }
   
}