using BffService.Domain.Interfaces;
using BffService.Domain.ValueObjects;
using BffService.Domain.Constants;
using BffService.Domain.Exceptions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace BffService.Infrastructure.Http;

public class ProxyService : IProxyService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ProxyService> _logger;
    private readonly Dictionary<string, string> _serviceUrls;

    public ProxyService(HttpClient httpClient, IConfiguration configuration, ILogger<ProxyService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
        
        _serviceUrls = new Dictionary<string, string>
        {
            { BffConstants.Proxy.Services.SensorService, _configuration["Services:SensorService:Url"] ?? "http://sensor-service:8080" },
            { BffConstants.Proxy.Services.ActuatorService, _configuration["Services:ActuatorService:Url"] ?? "http://actuator-service:8080" },
            { BffConstants.Proxy.Services.AuthService, _configuration["Services:AuthService:Url"] ?? "http://auth-service:8080" }
        };
    }

    public async Task<ProxyResponse> ForwardRequestAsync(
        ProxyRequest request, 
        string? accessToken, 
        string targetService, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!_serviceUrls.TryGetValue(targetService, out var serviceUrl))
            {
                throw new ProxyException($"Unknown target service: {targetService}");
            }

            _logger.LogInformation("Forwarding {Method} request to {Service}: {Path}", 
                request.Method, targetService, request.Path);

            // Add API prefix for the target service
            var apiPrefix = BffConstants.Proxy.ServiceApiPrefixes.GetValueOrDefault(targetService, "");
            var requestUri = $"{serviceUrl}{apiPrefix}{request.Path}";
            using var httpRequestMessage = new HttpRequestMessage(new HttpMethod(request.Method), requestUri);

            // Add authorization header if access token is provided
            if (!string.IsNullOrEmpty(accessToken))
            {
                httpRequestMessage.Headers.Add(BffConstants.Headers.Authorization, $"Bearer {accessToken}");
            }

            // Add request headers (excluding authorization to prevent override)
            foreach (var header in request.Headers.Where(h => 
                !h.Key.Equals(BffConstants.Headers.Authorization, StringComparison.OrdinalIgnoreCase)))
            {
                if (IsAllowedHeader(header.Key))
                {
                    httpRequestMessage.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }
            }

            // Add body if present
            if (!string.IsNullOrEmpty(request.Body))
            {
                httpRequestMessage.Content = new StringContent(
                    request.Body, 
                    Encoding.UTF8, 
                    request.Headers.GetValueOrDefault(BffConstants.Headers.ContentType, "application/json"));
            }

            var response = await _httpClient.SendAsync(httpRequestMessage, cancellationToken);

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            var responseHeaders = response.Headers
                .Concat(response.Content.Headers)
                .Where(h => IsAllowedResponseHeader(h.Key))
                .ToDictionary(h => h.Key, h => string.Join(", ", h.Value));

            _logger.LogInformation("Received response from {Service}: {StatusCode}", 
                targetService, response.StatusCode);

            return new ProxyResponse(
                (int)response.StatusCode,
                responseHeaders,
                responseBody
            );
        }
        catch (Exception ex) when (ex is not ProxyException)
        {
            _logger.LogError(ex, "Error forwarding request to {Service}: {Path}", targetService, request.Path);
            throw new ProxyException($"Failed to forward request to {targetService}", ex);
        }
    }

    public bool IsValidProxyPath(string path)
    {
        if (string.IsNullOrEmpty(path) || !path.StartsWith(BffConstants.Proxy.ProxyBasePath))
        {
            return false;
        }

        var servicePath = path.Substring(BffConstants.Proxy.ProxyBasePath.Length);
        
        return BffConstants.Proxy.ServiceRoutes.Keys.Any(route => 
            servicePath.StartsWith(route, StringComparison.OrdinalIgnoreCase));
    }

    public string GetTargetService(string path)
    {
        if (!IsValidProxyPath(path))
        {
            throw new ProxyException($"Invalid proxy path: {path}");
        }

        var servicePath = path.Substring(BffConstants.Proxy.ProxyBasePath.Length);
        
        var matchedRoute = BffConstants.Proxy.ServiceRoutes
            .FirstOrDefault(route => servicePath.StartsWith(route.Key, StringComparison.OrdinalIgnoreCase));

        if (matchedRoute.Key == null)
        {
            throw new ProxyException($"No service mapping found for path: {path}");
        }

        return matchedRoute.Value;
    }


    private static bool IsAllowedHeader(string headerName)
    {
        var disallowedHeaders = new[]
        {
            "host",
            "authorization",
            "connection",
            "upgrade",
            "proxy-authorization",
            "proxy-authenticate",
            "te",
            "trailer",
            "transfer-encoding"
        };

        return !disallowedHeaders.Contains(headerName.ToLowerInvariant());
    }

    private static bool IsAllowedResponseHeader(string headerName)
    {
        var disallowedResponseHeaders = new[]
        {
            "connection",
            "upgrade",
            "proxy-authenticate",
            "proxy-authorization",
            "te",
            "trailer",
            "transfer-encoding"
        };

        return !disallowedResponseHeaders.Contains(headerName.ToLowerInvariant());
    }
}