namespace AuthService.Application.Interfaces;

public interface IDeletePermissionUseCase
{
    Task<bool> ExecuteAsync(string id);
}