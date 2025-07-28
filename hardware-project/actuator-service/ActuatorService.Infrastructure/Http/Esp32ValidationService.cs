using ActuatorService.Application.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace ActuatorService.Infrastructure.Http;
public class Esp32ValidationService : IEsp32ValidationService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<Esp32ValidationService> _logger;

    public Esp32ValidationService(HttpClient httpClient, ILogger<Esp32ValidationService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<bool> ExistsAsync(string id)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/esp32nodes/{id}/exists");

            if (!response.IsSuccessStatusCode)
                return false;

            var content = await response.Content.ReadAsStringAsync();
            var json = JsonSerializer.Deserialize<JsonElement>(content);

            return json.GetProperty("exists").GetBoolean();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating ESP32 existence");
            return false;
        }
    }
}
