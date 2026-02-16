using BffService.Domain.DTOs;

namespace BffService.Application.Interfaces;

/// <summary>
/// Contract for the system status service that checks health of backend microservices.
/// </summary>
public interface ISystemStatusService
{
    Task<SystemStatusDto> GetSystemStatusAsync(string? accessToken, CancellationToken cancellationToken = default);
}
