using ActuatorService.Application.Interfaces;
using HydroEspinaca.Shared.Authentication.Services;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace ActuatorService.Infrastructure.Http;
public class Esp32ValidationService : IEsp32ValidationService
{
    private readonly HttpClient _httpClient;
    private readonly M2MTokenService _tokenService;
    private readonly ILogger<Esp32ValidationService> _logger;

    public Esp32ValidationService(
        HttpClient httpClient, 
        M2MTokenService tokenService,
        ILogger<Esp32ValidationService> logger)
    {
        _httpClient = httpClient;
        _tokenService = tokenService;
        _logger = logger;
    }

    public async Task<bool> ExistsAsync(string id)
    {
        try
        {
            // Configure HttpClient with M2M authentication
            await _tokenService.ConfigureHttpClientAsync(_httpClient);

            var response = await _httpClient.GetAsync($"/api/esp32nodes/{id}/exists");

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("ESP32 validation request failed with status {StatusCode} for ID {Id}", 
                    response.StatusCode, id);
                return false;
            }

            var content = await response.Content.ReadAsStringAsync();
            var json = JsonSerializer.Deserialize<JsonElement>(content);

            return json.GetProperty("exists").GetBoolean();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating ESP32 existence for ID {Id}", id);
            return false;
        }
    }
}
