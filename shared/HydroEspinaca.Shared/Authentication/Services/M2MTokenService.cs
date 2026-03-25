using HydroEspinaca.Shared.DTOs.Authentication;
using HydroEspinaca.Shared.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;

namespace HydroEspinaca.Shared.Authentication.Services;

/// <summary>
/// Service for obtaining M2M tokens from auth-service
/// </summary>
public class M2MTokenService : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly M2MAuthOptions _options;
    private readonly ILogger<M2MTokenService> _logger;
    private readonly SemaphoreSlim _tokenSemaphore = new(1, 1);
    
    private string? _cachedToken;
    private DateTime _tokenExpiresAt = DateTime.MinValue;
    private bool _disposed;

    public M2MTokenService(
        HttpClient httpClient,
        IOptions<M2MAuthOptions> options,
        ILogger<M2MTokenService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
        
        _options.Validate();
    }

    /// <summary>
    /// Gets a valid M2M access token (cached if not expired)
    /// </summary>
    public async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        // Check if we have a valid cached token
        if (!string.IsNullOrEmpty(_cachedToken) && DateTime.UtcNow < _tokenExpiresAt)
        {
            return _cachedToken;
        }

        await _tokenSemaphore.WaitAsync(cancellationToken);
        
        try
        {
            // Double-check after acquiring semaphore
            if (!string.IsNullOrEmpty(_cachedToken) && DateTime.UtcNow < _tokenExpiresAt)
            {
                return _cachedToken;
            }

            _logger.LogDebug("Requesting new M2M token for client {ClientId}", _options.ClientId);

            // Request new token
            var tokenResponse = await RequestTokenAsync(cancellationToken);
            
            // Cache the token with buffer time
            _cachedToken = tokenResponse.AccessToken;
            _tokenExpiresAt = DateTime.UtcNow.AddMinutes(_options.TokenCacheDurationMinutes);
            
            _logger.LogDebug("Successfully obtained M2M token, expires at {ExpiresAt}", _tokenExpiresAt);
            
            return _cachedToken;
        }
        finally
        {
            _tokenSemaphore.Release();
        }
    }

    /// <summary>
    /// Forces a refresh of the cached token
    /// </summary>
    public async Task<string> RefreshTokenAsync(CancellationToken cancellationToken = default)
    {
        await _tokenSemaphore.WaitAsync(cancellationToken);
        
        try
        {
            _logger.LogDebug("Force refreshing M2M token for client {ClientId}", _options.ClientId);
            
            // Clear cache
            _cachedToken = null;
            _tokenExpiresAt = DateTime.MinValue;
            
            // Get fresh token
            return await GetAccessTokenAsync(cancellationToken);
        }
        finally
        {
            _tokenSemaphore.Release();
        }
    }

    private async Task<TokenResult> RequestTokenAsync(CancellationToken cancellationToken)
    {
        try
        {
            var request = new ClientCredentialsRequest
            {
                ClientId = _options.ClientId,
                ClientSecret = _options.ClientSecret
            };

            var tokenUrl = _options.GetTokenUrl();
            _logger.LogDebug("[M2M] Requesting token from {Url} for client {ClientId}",
                tokenUrl, _options.ClientId);

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(tokenUrl, content, cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("[M2M] Token request failed with status {StatusCode} for client {ClientId}: {Error}", 
                    response.StatusCode, _options.ClientId, errorContent);
                
                throw new InvalidOperationException($"M2M token request failed: {response.StatusCode}");
            }

            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            var tokenResult = JsonSerializer.Deserialize<TokenResult>(responseJson, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            if (tokenResult == null || string.IsNullOrEmpty(tokenResult.AccessToken))
            {
                throw new InvalidOperationException("Invalid token response received");
            }

            _logger.LogDebug("[M2M] Token obtained successfully for client {ClientId}", _options.ClientId);
            return tokenResult;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "[M2M] Network error requesting token from {Url} for client {ClientId}",
                _options.GetTokenUrl(), _options.ClientId);
            throw new InvalidOperationException("Failed to request M2M token due to network error", ex);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "[M2M] Failed to deserialize token response for client {ClientId}",
                _options.ClientId);
            throw new InvalidOperationException("Failed to deserialize M2M token response", ex);
        }
    }

    /// <summary>
    /// Adds the M2M token as Authorization header to an HttpClient
    /// </summary>
    public async Task<HttpClient> ConfigureHttpClientAsync(
        HttpClient httpClient, 
        CancellationToken cancellationToken = default)
    {
        var token = await GetAccessTokenAsync(cancellationToken);
        httpClient.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        
        return httpClient;
    }

    /// <summary>
    /// Disposes managed resources (SemaphoreSlim)
    /// </summary>
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Protected implementation of Dispose pattern
    /// </summary>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _tokenSemaphore?.Dispose();
            }

            _disposed = true;
        }
    }
}

/// <summary>
/// Request DTO for M2M token
/// </summary>
public class ClientCredentialsRequest
{
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
}