using BffService.Domain.ValueObjects;

namespace BffService.Domain.Interfaces;

public interface IProxyService
{
    Task<ProxyResponse> ForwardRequestAsync(ProxyRequest request, string? accessToken, string targetService, CancellationToken cancellationToken = default);
    bool IsValidProxyPath(string path);
    string GetTargetService(string path);
    bool IsPublicRoute(string path);
}