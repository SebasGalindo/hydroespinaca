using BffService.Domain.DTOs;

namespace BffService.Application.Interfaces;

public interface ISystemStatusService
{
    Task<SystemStatusDto> GetSystemStatusAsync(string? accessToken, CancellationToken cancellationToken = default);
}
