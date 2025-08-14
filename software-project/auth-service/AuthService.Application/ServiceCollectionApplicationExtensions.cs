using AuthService.Application.Interfaces;
using AuthService.Application.Mappers;
using AuthService.Application.UseCases;
using AuthService.Application.Validators;
using AuthService.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace AuthService.Application;

public static class ServiceCollectionApplicationExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddAutoMapper(cfg => { },
             typeof(UserMappingProfile),
             typeof(RefreshMappingProfile),
             typeof(DomainMappingProfile));

        services.AddScoped<IAuthenticateUserUseCase, AuthenticateUserUseCase>();
        services.AddScoped<IClientCredentialsUseCase, ClientCredentialsUseCase>();
        services.AddScoped<IRefreshTokenUseCase, RefreshTokenUseCase>();

        // Permission Use Cases
        services.AddScoped<ICreatePermissionUseCase, CreatePermissionUseCase>();
        services.AddScoped<IGetPermissionUseCase, GetPermissionUseCase>();
        services.AddScoped<IGetAllPermissionsUseCase, GetAllPermissionsUseCase>();
        services.AddScoped<IUpdatePermissionUseCase, UpdatePermissionUseCase>();
        services.AddScoped<IDeletePermissionUseCase, DeletePermissionUseCase>();

        // Role Use Cases
        services.AddScoped<ICreateRoleUseCase, CreateRoleUseCase>();
        services.AddScoped<IGetRoleUseCase, GetRoleUseCase>();
        services.AddScoped<IGetAllRolesUseCase, GetAllRolesUseCase>();
        services.AddScoped<IUpdateRoleUseCase, UpdateRoleUseCase>();
        services.AddScoped<IDeleteRoleUseCase, DeleteRoleUseCase>();

        // Validators
        services.AddScoped<CreatePermissionRequestValidator>();
        services.AddScoped<UpdatePermissionRequestValidator>();
        services.AddScoped<CreateRoleRequestValidator>();
        services.AddScoped<UpdateRoleRequestValidator>();

      

        return services;
    }
}
