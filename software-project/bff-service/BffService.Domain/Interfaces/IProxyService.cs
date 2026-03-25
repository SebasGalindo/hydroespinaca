using BffService.Domain.ValueObjects;

namespace BffService.Domain.Interfaces;

/// <summary>
/// Contract for the proxy service that forwards HTTP requests to backend microservices.
/// </summary>
public interface IProxyService
{
    Task<ProxyResponse> ForwardRequestAsync(ProxyRequest request, string? accessToken, string targetService, CancellationToken cancellationToken = default);
    bool IsValidProxyPath(string path);
    string GetTargetService(string path);
}