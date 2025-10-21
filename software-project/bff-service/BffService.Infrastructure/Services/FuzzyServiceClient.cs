using BffService.Application.Interfaces;
using BffService.Domain.DTOs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace BffService.Infrastructure.Services;

/// <summary>
/// HTTP client for communicating with the fuzzy-service microservice
/// </summary>
public class FuzzyServiceClient : IFuzzyServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<FuzzyServiceClient> _logger;
    private readonly string _baseUrl;
    private readonly JsonSerializerOptions _jsonOptions;

    public FuzzyServiceClient(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<FuzzyServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;

        // Read fuzzy-service URL from configuration or use default
        _baseUrl = configuration["Services:FuzzyService:Url"] ?? "http://fuzzy-service:8000";

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    public async Task<List<FuzzyRuleSummaryDto>> GetRulesSummaryAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Fetching fuzzy rules summary from fuzzy-service");

            var url = $"{_baseUrl}/api/fuzzy-rules/summary/name-description";
            _logger.LogDebug("Fuzzy-service URL: {Url}", url);

            var response = await _httpClient.GetAsync(url, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Fuzzy-service API returned status {StatusCode}: {ErrorContent}",
                    response.StatusCode, errorContent);
                response.EnsureSuccessStatusCode();
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogDebug("Fuzzy-service API response: {Content}", content);

            var rules = JsonSerializer.Deserialize<List<FuzzyRuleSummaryDto>>(content, _jsonOptions);

            if (rules == null)
            {
                _logger.LogWarning("Fuzzy-service returned null response, returning empty list");
                return new List<FuzzyRuleSummaryDto>();
            }

            _logger.LogInformation("Successfully fetched {Count} fuzzy rules", rules.Count);
            return rules;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error fetching fuzzy rules summary");
            throw;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Error deserializing fuzzy rules response");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error fetching fuzzy rules summary");
            throw;
        }
    }
}
