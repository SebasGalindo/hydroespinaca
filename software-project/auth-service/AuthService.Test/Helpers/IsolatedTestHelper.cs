// No Application layer dependencies - tests should work with HTTP responses directly
using AuthService.Test.Fixtures;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace AuthService.Test.Helpers;

/// <summary>
/// Test helper that creates fresh, isolated clients for each test method
/// to prevent state sharing between tests
/// </summary>
public class IsolatedTestHelper : IDisposable
{
    private readonly IntegrationTestBase _factory;
    private readonly List<HttpClient> _clients = new();

    public IsolatedTestHelper(IntegrationTestBase factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// Creates a fresh HttpClient with authentication token for isolated testing
    /// </summary>
    public async Task<HttpClient> CreateAuthenticatedClientAsync(bool isAdmin = true)
    {
        var client = _factory.CreateFreshClient();
        _clients.Add(client);

        // Get token using a separate AuthTestHelper instance
        using var authHelper = new AuthTestHelper(client, _factory.Services, _factory.Database);
        
        var token = isAdmin 
            ? await authHelper.GetAdminTokenAsync() 
            : await authHelper.GetUserTokenAsync();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        
        return client;
    }

    /// <summary>
    /// Creates a fresh unauthenticated HttpClient
    /// </summary>
    public HttpClient CreateUnauthenticatedClient()
    {
        var client = _factory.CreateFreshClient();
        _clients.Add(client);
        return client;
    }

    /// <summary>
    /// Makes an authenticated POST request with unique test data
    /// </summary>
    public async Task<HttpResponse<T>> PostAsJsonAsync<T>(
        HttpClient client, 
        string requestUri, 
        object content)
    {
        var json = JsonSerializer.Serialize(content);
        var response = await client.PostAsync(requestUri,
            new StringContent(json, Encoding.UTF8, "application/json"));

        var responseContent = await response.Content.ReadAsStringAsync();
        
        return new HttpResponse<T>
        {
            StatusCode = response.StatusCode,
            Content = responseContent,
            IsSuccess = response.IsSuccessStatusCode,
            Data = response.IsSuccessStatusCode && !string.IsNullOrEmpty(responseContent) 
                ? JsonSerializer.Deserialize<T>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                : default
        };
    }

    public void Dispose()
    {
        foreach (var client in _clients)
        {
            client.Dispose();
        }
        _clients.Clear();
    }
}

public class HttpResponse<T>
{
    public System.Net.HttpStatusCode StatusCode { get; set; }
    public string Content { get; set; } = string.Empty;
    public bool IsSuccess { get; set; }
    public T? Data { get; set; }
}