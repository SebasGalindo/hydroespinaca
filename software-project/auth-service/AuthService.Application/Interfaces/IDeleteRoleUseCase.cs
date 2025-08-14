namespace AuthService.Application.Interfaces;

public interface IDeleteRoleUseCase
{
    Task<bool> ExecuteAsync(string id);
}