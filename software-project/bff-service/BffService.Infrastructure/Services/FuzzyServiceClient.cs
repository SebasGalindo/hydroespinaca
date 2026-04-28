using BffService.Application.Interfaces;
using BffService.Domain.DTOs;
using BffService.Domain.DTOs.Fuzzy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;


namespace BffService.Infrastructure.Services;

/// <summary>
/// HTTP client for communicating with the fuzzy-service microservice.
/// Follows the same typed HttpClient pattern as BiServiceClient.
/// Forwards the user's access token for authorization on fuzzy-service.
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
        _baseUrl = configuration["Services:FuzzyService:Url"] ?? "http://fuzzy-service:8000";

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        };
    }

    // ──────────────────────────────────────────────
    //  Legacy (sin token – endpoint público cacheado)
    // ──────────────────────────────────────────────

    public async Task<List<FuzzyRuleSummaryDto>> GetRulesSummaryAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Fetching fuzzy rules summary from fuzzy-service");

            var url = $"{_baseUrl}/api/fuzzy-rules/summary/name-description";
            var response = await _httpClient.GetAsync(url, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Fuzzy-service returned status {StatusCode}: {ErrorContent}",
                    response.StatusCode, errorContent);
                response.EnsureSuccessStatusCode();
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var rules = JsonSerializer.Deserialize<List<FuzzyRuleSummaryDto>>(content, _jsonOptions);

            if (rules == null)
            {
                _logger.LogWarning("Fuzzy-service returned null response, returning empty list");
                return new List<FuzzyRuleSummaryDto>();
            }

            _logger.LogInformation("Successfully fetched {Count} fuzzy rules", rules.Count);
            return rules;
        }
        catch (HttpRequestException ex) { _logger.LogError(ex, "HTTP error fetching fuzzy rules summary"); throw; }
        catch (JsonException ex) { _logger.LogError(ex, "Error deserializing fuzzy rules response"); throw; }
        catch (Exception ex) { _logger.LogError(ex, "Unexpected error fetching fuzzy rules summary"); throw; }
    }

    // ──────────────────────────────────────────────
    //  Fuzzy Systems
    // ──────────────────────────────────────────────

    public async Task<List<FuzzySystemDto>> GetSystemsAsync(
        string accessToken, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Fetching all fuzzy systems");
            var request = CreateRequest(HttpMethod.Get, "/api/fuzzy-systems", accessToken);
            var response = await _httpClient.SendAsync(request, cancellationToken);
            await EnsureSuccessOrThrow(response, "getting fuzzy systems", cancellationToken);

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<List<FuzzySystemDto>>(content, _jsonOptions)
                ?? new List<FuzzySystemDto>();
        }
        catch (HttpRequestException ex) { _logger.LogError(ex, "HTTP error fetching fuzzy systems"); throw; }
        catch (Exception ex) when (ex is not HttpRequestException) { _logger.LogError(ex, "Unexpected error fetching fuzzy systems"); throw; }
    }

    public async Task<FuzzySystemDto?> GetSystemByIdAsync(
        string accessToken, string id, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Fetching fuzzy system {Id}", id);
            var request = CreateRequest(HttpMethod.Get, $"/api/fuzzy-systems/{id}", accessToken);
            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return null;

            await EnsureSuccessOrThrow(response, "getting fuzzy system by id", cancellationToken);

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<FuzzySystemDto>(content, _jsonOptions);
        }
                catch (HttpRequestException ex) { _logger.LogError(ex, "HTTP error fetching fuzzy system {Id}", id); throw; }
        catch (Exception ex) when (ex is not HttpRequestException) { _logger.LogError(ex, "Unexpected error fetching fuzzy system {Id}", id); throw; }
    }

    public async Task<FuzzySystemDto> CreateSystemAsync(
        string accessToken, CreateFuzzySystemRequest createRequest,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Creating fuzzy system with name: {Name}", createRequest.Name);
            var request = CreateRequest(HttpMethod.Post, "/api/fuzzy-systems", accessToken);
            request.Content = CreateJsonContent(createRequest);

            var response = await _httpClient.SendAsync(request, cancellationToken);
            await EnsureSuccessOrThrow(response, "creating fuzzy system", cancellationToken);

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<FuzzySystemDto>(content, _jsonOptions)
                ?? throw new InvalidOperationException("fuzzy-service returned null for created system");
        }
        catch (HttpRequestException ex) { _logger.LogError(ex, "HTTP error creating fuzzy system"); throw; }
        catch (Exception ex) when (ex is not HttpRequestException and not InvalidOperationException)
        { _logger.LogError(ex, "Unexpected error creating fuzzy system"); throw; }
    }

    public async Task<FuzzySystemDto> UpdateSystemAsync(
        string accessToken, string id, UpdateFuzzySystemRequest updateRequest,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Updating fuzzy system {Id}", id);
            var request = CreateRequest(HttpMethod.Put, $"/api/fuzzy-systems/{id}", accessToken);
            request.Content = CreateJsonContent(updateRequest);

            var response = await _httpClient.SendAsync(request, cancellationToken);
            await EnsureSuccessOrThrow(response, "updating fuzzy system", cancellationToken);

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<FuzzySystemDto>(content, _jsonOptions)
                ?? throw new InvalidOperationException("fuzzy-service returned null for updated system");
        }
        catch (HttpRequestException ex) { _logger.LogError(ex, "HTTP error updating fuzzy system {Id}", id); throw; }
        catch (Exception ex) when (ex is not HttpRequestException and not InvalidOperationException)
        { _logger.LogError(ex, "Unexpected error updating fuzzy system {Id}", id); throw; }
    }

    public async Task<FuzzySystemDto> UpdateSystemStatusAsync(
        string accessToken, string id, UpdateFuzzySystemStatusRequest statusRequest,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Updating fuzzy system {Id} status to {Status}", id, statusRequest.Status);
            var request = CreateRequest(HttpMethod.Patch, $"/api/fuzzy-systems/{id}/status", accessToken);
            request.Content = CreateJsonContent(statusRequest);

            var response = await _httpClient.SendAsync(request, cancellationToken);
            await EnsureSuccessOrThrow(response, "updating fuzzy system status", cancellationToken);

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<FuzzySystemDto>(content, _jsonOptions)
                ?? throw new InvalidOperationException("fuzzy-service returned null for status update");
        }
        catch (HttpRequestException ex) { _logger.LogError(ex, "HTTP error updating fuzzy system status {Id}", id); throw; }
        catch (Exception ex) when (ex is not HttpRequestException and not InvalidOperationException)
        { _logger.LogError(ex, "Unexpected error updating fuzzy system status {Id}", id); throw; }
    }

    public async Task<FuzzySystemDto> ActivateSystemAsync(
        string accessToken, string id, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Activating fuzzy system {Id}", id);
            var request = CreateRequest(HttpMethod.Post, $"/api/fuzzy-systems/{id}/activate", accessToken);
            var response = await _httpClient.SendAsync(request, cancellationToken);
            await EnsureSuccessOrThrow(response, "activating fuzzy system", cancellationToken);

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<FuzzySystemDto>(content, _jsonOptions)
                ?? throw new InvalidOperationException("fuzzy-service returned null for activated system");
        }
        catch (HttpRequestException ex) { _logger.LogError(ex, "HTTP error activating fuzzy system {Id}", id); throw; }
        catch (Exception ex) when (ex is not HttpRequestException and not InvalidOperationException)
        { _logger.LogError(ex, "Unexpected error activating fuzzy system {Id}", id); throw; }
    }

    public async Task<FuzzySystemDto> CloneSystemAsync(
        string accessToken, string id, CloneFuzzySystemRequest? cloneRequest = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Cloning fuzzy system {Id}", id);
            var request = CreateRequest(HttpMethod.Post, $"/api/fuzzy-systems/{id}/clone", accessToken);

            if (cloneRequest != null)
            {
                request.Content = new StringContent(
                    JsonSerializer.Serialize(cloneRequest, _jsonOptions),
                    Encoding.UTF8, "application/json");
            }

            var response = await _httpClient.SendAsync(request, cancellationToken);
            await EnsureSuccessOrThrow(response, "cloning fuzzy system", cancellationToken);

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<FuzzySystemDto>(content, _jsonOptions)
                ?? throw new InvalidOperationException("fuzzy-service returned null for cloned system");
        }
        catch (HttpRequestException ex) { _logger.LogError(ex, "HTTP error cloning fuzzy system {Id}", id); throw; }
        catch (Exception ex) when (ex is not HttpRequestException and not InvalidOperationException)
        { _logger.LogError(ex, "Unexpected error cloning fuzzy system {Id}", id); throw; }
    }

    public async Task<Dictionary<string, object?>> ExportSystemAsync(
        string accessToken, string id, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Exporting fuzzy system {Id}", id);
            var request = CreateRequest(HttpMethod.Get, $"/api/fuzzy-systems/{id}/export", accessToken);
            var response = await _httpClient.SendAsync(request, cancellationToken);
            await EnsureSuccessOrThrow(response, "exporting fuzzy system", cancellationToken);

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<Dictionary<string, object?>>(content, _jsonOptions)
                ?? throw new InvalidOperationException("fuzzy-service returned null for export");
        }
        catch (HttpRequestException ex) { _logger.LogError(ex, "HTTP error exporting fuzzy system {Id}", id); throw; }
        catch (Exception ex) when (ex is not HttpRequestException and not InvalidOperationException)
        { _logger.LogError(ex, "Unexpected error exporting fuzzy system {Id}", id); throw; }
    }

    public async Task<FuzzySystemDto> ImportSystemAsync(
        string accessToken, Dictionary<string, object?> exportData,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Importing fuzzy system from export data");
            var request = CreateRequest(HttpMethod.Post, "/api/fuzzy-systems/import", accessToken);
            request.Content = new StringContent(
                JsonSerializer.Serialize(exportData, _jsonOptions),
                Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request, cancellationToken);
            await EnsureSuccessOrThrow(response, "importing fuzzy system", cancellationToken);

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<FuzzySystemDto>(content, _jsonOptions)
                ?? throw new InvalidOperationException("fuzzy-service returned null for imported system");
        }
        catch (HttpRequestException ex) { _logger.LogError(ex, "HTTP error importing fuzzy system"); throw; }
        catch (Exception ex) when (ex is not HttpRequestException and not InvalidOperationException)
        { _logger.LogError(ex, "Unexpected error importing fuzzy system"); throw; }
    }

    public async Task<SimulateFuzzySystemResponse> SimulateSystemAsync(
        string accessToken, string id, SimulateFuzzySystemRequest simulateRequest,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Simulating fuzzy system {Id}", id);
            var request = CreateRequest(HttpMethod.Post, $"/api/fuzzy-systems/{id}/simulate", accessToken);
            request.Content = new StringContent(
                JsonSerializer.Serialize(simulateRequest, _jsonOptions),
                Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request, cancellationToken);
            await EnsureSuccessOrThrow(response, "simulating fuzzy system", cancellationToken);

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<SimulateFuzzySystemResponse>(content, _jsonOptions)
                ?? throw new InvalidOperationException("fuzzy-service returned null for simulation");
        }
        catch (HttpRequestException ex) { _logger.LogError(ex, "HTTP error simulating fuzzy system {Id}", id); throw; }
        catch (Exception ex) when (ex is not HttpRequestException and not InvalidOperationException)
        { _logger.LogError(ex, "Unexpected error simulating fuzzy system {Id}", id); throw; }
    }

    public async Task DeleteSystemAsync(
        string accessToken, string id, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Deleting fuzzy system {Id}", id);
            var request = CreateRequest(HttpMethod.Delete, $"/api/fuzzy-systems/{id}", accessToken);
            var response = await _httpClient.SendAsync(request, cancellationToken);
            await EnsureSuccessOrThrow(response, "deleting fuzzy system", cancellationToken);
        }
        catch (HttpRequestException ex) { _logger.LogError(ex, "HTTP error deleting fuzzy system {Id}", id); throw; }
        catch (Exception ex) when (ex is not HttpRequestException)
        { _logger.LogError(ex, "Unexpected error deleting fuzzy system {Id}", id); throw; }
    }

    // ──────────────────────────────────────────────
    //  Fuzzy Variables
    // ──────────────────────────────────────────────

    public async Task<List<FuzzyVariableDto>> GetVariablesBySystemAsync(
        string accessToken, string systemId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Fetching variables for system {SystemId}", systemId);
            var request = CreateRequest(HttpMethod.Get,
                $"/api/fuzzy-variables/system/{systemId}", accessToken);
            var response = await _httpClient.SendAsync(request, cancellationToken);
            await EnsureSuccessOrThrow(response, "getting variables by system", cancellationToken);

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<List<FuzzyVariableDto>>(content, _jsonOptions)
                ?? new List<FuzzyVariableDto>();
        }
        catch (HttpRequestException ex) { _logger.LogError(ex, "HTTP error fetching variables for system {Id}", systemId); throw; }
        catch (Exception ex) when (ex is not HttpRequestException)
        { _logger.LogError(ex, "Unexpected error fetching variables for system {Id}", systemId); throw; }
    }

    public async Task<FuzzyVariableDto> CreateVariableAsync(
        string accessToken, CreateFuzzyVariableRequest createRequest,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Creating fuzzy variable: {Name} for system {SystemId}",
                createRequest.Name, createRequest.SystemId);
            var request = CreateRequest(HttpMethod.Post, "/api/fuzzy-variables", accessToken);
            request.Content = CreateJsonContent(createRequest);

            var response = await _httpClient.SendAsync(request, cancellationToken);
            await EnsureSuccessOrThrow(response, "creating fuzzy variable", cancellationToken);

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<FuzzyVariableDto>(content, _jsonOptions)
                ?? throw new InvalidOperationException("fuzzy-service returned null for created variable");
        }
        catch (HttpRequestException ex) { _logger.LogError(ex, "HTTP error creating fuzzy variable"); throw; }
        catch (Exception ex) when (ex is not HttpRequestException and not InvalidOperationException)
        { _logger.LogError(ex, "Unexpected error creating fuzzy variable"); throw; }
    }

    public async Task<FuzzyVariableDto> UpdateVariableAsync(
        string accessToken, string id, UpdateFuzzyVariableRequest updateRequest,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Updating fuzzy variable {Id}", id);
            var request = CreateRequest(HttpMethod.Put, $"/api/fuzzy-variables/{id}", accessToken);
            request.Content = CreateJsonContent(updateRequest);

            var response = await _httpClient.SendAsync(request, cancellationToken);
            await EnsureSuccessOrThrow(response, "updating fuzzy variable", cancellationToken);

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<FuzzyVariableDto>(content, _jsonOptions)
                ?? throw new InvalidOperationException("fuzzy-service returned null for updated variable");
        }
        catch (HttpRequestException ex) { _logger.LogError(ex, "HTTP error updating fuzzy variable {Id}", id); throw; }
        catch (Exception ex) when (ex is not HttpRequestException and not InvalidOperationException)
        { _logger.LogError(ex, "Unexpected error updating fuzzy variable {Id}", id); throw; }
    }

    public async Task DeleteVariableAsync(
        string accessToken, string id, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Deleting fuzzy variable {Id}", id);
            var request = CreateRequest(HttpMethod.Delete, $"/api/fuzzy-variables/{id}", accessToken);
            var response = await _httpClient.SendAsync(request, cancellationToken);
            await EnsureSuccessOrThrow(response, "deleting fuzzy variable", cancellationToken);
        }
        catch (HttpRequestException ex) { _logger.LogError(ex, "HTTP error deleting fuzzy variable {Id}", id); throw; }
        catch (Exception ex) when (ex is not HttpRequestException)
        { _logger.LogError(ex, "Unexpected error deleting fuzzy variable {Id}", id); throw; }
    }

    // ──────────────────────────────────────────────
    //  Fuzzy Terms
    // ──────────────────────────────────────────────

    public async Task<List<FuzzyTermDto>> GetTermsByVariableAsync(
        string accessToken, string variableId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Fetching terms for variable {VariableId}", variableId);
            var request = CreateRequest(HttpMethod.Get,
                $"/api/fuzzy-terms?variable_id={variableId}", accessToken);
            var response = await _httpClient.SendAsync(request, cancellationToken);
            await EnsureSuccessOrThrow(response, "getting terms by variable", cancellationToken);

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<List<FuzzyTermDto>>(content, _jsonOptions)
                ?? new List<FuzzyTermDto>();
        }
        catch (HttpRequestException ex) { _logger.LogError(ex, "HTTP error fetching terms for variable {Id}", variableId); throw; }
        catch (Exception ex) when (ex is not HttpRequestException)
        { _logger.LogError(ex, "Unexpected error fetching terms for variable {Id}", variableId); throw; }
    }

    public async Task<FuzzyTermDto> CreateTermAsync(
        string accessToken, CreateFuzzyTermRequest createRequest,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Creating fuzzy term: {Label} for variable {VariableId}",
                createRequest.Label, createRequest.VariableId);
            var request = CreateRequest(HttpMethod.Post, "/api/fuzzy-terms", accessToken);
            request.Content = CreateJsonContent(createRequest);

            var response = await _httpClient.SendAsync(request, cancellationToken);
            await EnsureSuccessOrThrow(response, "creating fuzzy term", cancellationToken);

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<FuzzyTermDto>(content, _jsonOptions)
                ?? throw new InvalidOperationException("fuzzy-service returned null for created term");
        }
        catch (HttpRequestException ex) { _logger.LogError(ex, "HTTP error creating fuzzy term"); throw; }
        catch (Exception ex) when (ex is not HttpRequestException and not InvalidOperationException)
        { _logger.LogError(ex, "Unexpected error creating fuzzy term"); throw; }
    }

    public async Task<FuzzyTermDto> UpdateTermAsync(
        string accessToken, string id, UpdateFuzzyTermRequest updateRequest,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Updating fuzzy term {Id}", id);
            var request = CreateRequest(HttpMethod.Put, $"/api/fuzzy-terms/{id}", accessToken);
            request.Content = CreateJsonContent(updateRequest);

            var response = await _httpClient.SendAsync(request, cancellationToken);
            await EnsureSuccessOrThrow(response, "updating fuzzy term", cancellationToken);

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<FuzzyTermDto>(content, _jsonOptions)
                ?? throw new InvalidOperationException("fuzzy-service returned null for updated term");
        }
        catch (HttpRequestException ex) { _logger.LogError(ex, "HTTP error updating fuzzy term {Id}", id); throw; }
        catch (Exception ex) when (ex is not HttpRequestException and not InvalidOperationException)
        { _logger.LogError(ex, "Unexpected error updating fuzzy term {Id}", id); throw; }
    }

    public async Task DeleteTermAsync(
        string accessToken, string id, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Deleting fuzzy term {Id}", id);
            var request = CreateRequest(HttpMethod.Delete, $"/api/fuzzy-terms/{id}", accessToken);
            var response = await _httpClient.SendAsync(request, cancellationToken);
            await EnsureSuccessOrThrow(response, "deleting fuzzy term", cancellationToken);
        }
        catch (HttpRequestException ex) { _logger.LogError(ex, "HTTP error deleting fuzzy term {Id}", id); throw; }
        catch (Exception ex) when (ex is not HttpRequestException)
        { _logger.LogError(ex, "Unexpected error deleting fuzzy term {Id}", id); throw; }
    }

    // ──────────────────────────────────────────────
    //  Fuzzy Rules
    // ──────────────────────────────────────────────

    public async Task<List<FuzzyRuleDto>> GetRulesBySystemAsync(
        string accessToken, string systemId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Fetching rules for system {SystemId}", systemId);
            var request = CreateRequest(HttpMethod.Get,
                $"/api/fuzzy-rules/system/{systemId}", accessToken);
            var response = await _httpClient.SendAsync(request, cancellationToken);
            await EnsureSuccessOrThrow(response, "getting rules by system", cancellationToken);

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<List<FuzzyRuleDto>>(content, _jsonOptions)
                ?? new List<FuzzyRuleDto>();
        }
        catch (HttpRequestException ex) { _logger.LogError(ex, "HTTP error fetching rules for system {Id}", systemId); throw; }
        catch (Exception ex) when (ex is not HttpRequestException)
        { _logger.LogError(ex, "Unexpected error fetching rules for system {Id}", systemId); throw; }
    }

    public async Task<FuzzyRuleDto> CreateRuleAsync(
        string accessToken, CreateFuzzyRuleRequest createRequest,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Creating fuzzy rule: {Name} for system {SystemId}",
                createRequest.Name, createRequest.SystemId);
            var request = CreateRequest(HttpMethod.Post, "/api/fuzzy-rules", accessToken);
            request.Content = CreateJsonContent(createRequest);

            var response = await _httpClient.SendAsync(request, cancellationToken);
            await EnsureSuccessOrThrow(response, "creating fuzzy rule", cancellationToken);

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<FuzzyRuleDto>(content, _jsonOptions)
                ?? throw new InvalidOperationException("fuzzy-service returned null for created rule");
        }
        catch (HttpRequestException ex) { _logger.LogError(ex, "HTTP error creating fuzzy rule"); throw; }
        catch (Exception ex) when (ex is not HttpRequestException and not InvalidOperationException)
        { _logger.LogError(ex, "Unexpected error creating fuzzy rule"); throw; }
    }

    public async Task<FuzzyRuleDto> UpdateRuleAsync(
        string accessToken, string id, UpdateFuzzyRuleRequest updateRequest,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Updating fuzzy rule {Id}", id);
            var request = CreateRequest(HttpMethod.Put, $"/api/fuzzy-rules/{id}", accessToken);
            request.Content = CreateJsonContent(updateRequest);

            var response = await _httpClient.SendAsync(request, cancellationToken);
            await EnsureSuccessOrThrow(response, "updating fuzzy rule", cancellationToken);

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<FuzzyRuleDto>(content, _jsonOptions)
                ?? throw new InvalidOperationException("fuzzy-service returned null for updated rule");
        }
        catch (HttpRequestException ex) { _logger.LogError(ex, "HTTP error updating fuzzy rule {Id}", id); throw; }
        catch (Exception ex) when (ex is not HttpRequestException and not InvalidOperationException)
        { _logger.LogError(ex, "Unexpected error updating fuzzy rule {Id}", id); throw; }
    }

    public async Task DeleteRuleAsync(
        string accessToken, string id, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Deleting fuzzy rule {Id}", id);
            var request = CreateRequest(HttpMethod.Delete, $"/api/fuzzy-rules/{id}", accessToken);
            var response = await _httpClient.SendAsync(request, cancellationToken);
            await EnsureSuccessOrThrow(response, "deleting fuzzy rule", cancellationToken);
        }
        catch (HttpRequestException ex) { _logger.LogError(ex, "HTTP error deleting fuzzy rule {Id}", id); throw; }
        catch (Exception ex) when (ex is not HttpRequestException)
        { _logger.LogError(ex, "Unexpected error deleting fuzzy rule {Id}", id); throw; }
    }

    // ──────────────────────────────────────────────
    //  Fuzzy Evaluations
    // ──────────────────────────────────────────────

    public async Task<FuzzyEvaluationsListResponseDto> GetEvaluationsAsync(
        string accessToken,
        string? systemId,
        DateTime? startDate,
        DateTime? endDate,
        int page,
        int pageSize,
        string sortBy,
        string sortOrder,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = BuildEvaluationsQuery(systemId, startDate, endDate, page, pageSize, sortBy, sortOrder);
            _logger.LogInformation("Fetching fuzzy evaluations {Query}", query);
            var request = CreateRequest(HttpMethod.Get, $"/api/fuzzy-evaluations/{query}", accessToken);
            var response = await _httpClient.SendAsync(request, cancellationToken);
            await EnsureSuccessOrThrow(response, "getting fuzzy evaluations", cancellationToken);

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<FuzzyEvaluationsListResponseDto>(content, _jsonOptions)
                ?? new FuzzyEvaluationsListResponseDto();
        }
        catch (HttpRequestException ex) { _logger.LogError(ex, "HTTP error fetching fuzzy evaluations"); throw; }
        catch (Exception ex) when (ex is not HttpRequestException) { _logger.LogError(ex, "Unexpected error fetching fuzzy evaluations"); throw; }
    }

    public async Task<FuzzyEvaluationsListResponseDto> GetRecentEvaluationsAsync(
        string accessToken,
        int hours,
        string? systemId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var parts = new List<string> { $"hours={hours}", $"page={page}", $"page_size={pageSize}" };
            if (!string.IsNullOrWhiteSpace(systemId))
                parts.Add($"system_id={Uri.EscapeDataString(systemId)}");
            var query = "?" + string.Join("&", parts);

            _logger.LogInformation("Fetching recent fuzzy evaluations {Query}", query);
            var request = CreateRequest(HttpMethod.Get, $"/api/fuzzy-evaluations/recent{query}", accessToken);
            var response = await _httpClient.SendAsync(request, cancellationToken);
            await EnsureSuccessOrThrow(response, "getting recent fuzzy evaluations", cancellationToken);

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<FuzzyEvaluationsListResponseDto>(content, _jsonOptions)
                ?? new FuzzyEvaluationsListResponseDto();
        }
        catch (HttpRequestException ex) { _logger.LogError(ex, "HTTP error fetching recent fuzzy evaluations"); throw; }
        catch (Exception ex) when (ex is not HttpRequestException) { _logger.LogError(ex, "Unexpected error fetching recent fuzzy evaluations"); throw; }
    }

    public async Task<FuzzyEvaluationStatsDto> GetEvaluationStatsAsync(
        string accessToken,
        string? systemId,
        int days,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var parts = new List<string> { $"days={days}" };
            if (!string.IsNullOrWhiteSpace(systemId))
                parts.Add($"system_id={Uri.EscapeDataString(systemId)}");
            var query = "?" + string.Join("&", parts);

            _logger.LogInformation("Fetching fuzzy evaluation stats {Query}", query);
            var request = CreateRequest(HttpMethod.Get, $"/api/fuzzy-evaluations/stats/summary{query}", accessToken);
            var response = await _httpClient.SendAsync(request, cancellationToken);
            await EnsureSuccessOrThrow(response, "getting fuzzy evaluation stats", cancellationToken);

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<FuzzyEvaluationStatsDto>(content, _jsonOptions)
                ?? new FuzzyEvaluationStatsDto();
        }
        catch (HttpRequestException ex) { _logger.LogError(ex, "HTTP error fetching fuzzy evaluation stats"); throw; }
        catch (Exception ex) when (ex is not HttpRequestException) { _logger.LogError(ex, "Unexpected error fetching fuzzy evaluation stats"); throw; }
    }

    public async Task<FuzzyEvaluationsListResponseDto> GetEvaluationsBySystemAsync(
        string accessToken,
        string systemId,
        DateTime? startDate,
        DateTime? endDate,
        int page,
        int pageSize,
        string sortOrder,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var parts = new List<string>
            {
                $"page={page}",
                $"page_size={pageSize}",
                $"sort_order={Uri.EscapeDataString(sortOrder)}",
            };
            if (startDate.HasValue)
                parts.Add($"start_date={Uri.EscapeDataString(startDate.Value.ToUniversalTime().ToString("o"))}");
            if (endDate.HasValue)
                parts.Add($"end_date={Uri.EscapeDataString(endDate.Value.ToUniversalTime().ToString("o"))}");
            var query = "?" + string.Join("&", parts);

            _logger.LogInformation("Fetching fuzzy evaluations for system {SystemId} {Query}", systemId, query);
            var request = CreateRequest(HttpMethod.Get,
                $"/api/fuzzy-evaluations/system/{Uri.EscapeDataString(systemId)}{query}", accessToken);
            var response = await _httpClient.SendAsync(request, cancellationToken);
            await EnsureSuccessOrThrow(response, "getting fuzzy evaluations by system", cancellationToken);

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<FuzzyEvaluationsListResponseDto>(content, _jsonOptions)
                ?? new FuzzyEvaluationsListResponseDto();
        }
        catch (HttpRequestException ex) { _logger.LogError(ex, "HTTP error fetching fuzzy evaluations for system {Id}", systemId); throw; }
        catch (Exception ex) when (ex is not HttpRequestException) { _logger.LogError(ex, "Unexpected error fetching fuzzy evaluations for system {Id}", systemId); throw; }
    }

    private static string BuildEvaluationsQuery(
        string? systemId,
        DateTime? startDate,
        DateTime? endDate,
        int page,
        int pageSize,
        string sortBy,
        string sortOrder)
    {
        var parts = new List<string>
        {
            $"page={page}",
            $"page_size={pageSize}",
            $"sort_by={Uri.EscapeDataString(sortBy)}",
            $"sort_order={Uri.EscapeDataString(sortOrder)}",
        };
        if (!string.IsNullOrWhiteSpace(systemId))
            parts.Add($"system_id={Uri.EscapeDataString(systemId)}");
        if (startDate.HasValue)
            parts.Add($"start_date={Uri.EscapeDataString(startDate.Value.ToUniversalTime().ToString("o"))}");
        if (endDate.HasValue)
            parts.Add($"end_date={Uri.EscapeDataString(endDate.Value.ToUniversalTime().ToString("o"))}");
        return "?" + string.Join("&", parts);
    }

    // ──────────────────────────────────────────────
    //  Private Helpers
    // ──────────────────────────────────────────────

    private HttpRequestMessage CreateRequest(HttpMethod method, string path, string accessToken)
    {
        var url = $"{_baseUrl}{path}";
        _logger.LogDebug("Fuzzy-service request: {Method} {Url}", method, url);

        var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return request;
    }

    private StringContent CreateJsonContent<T>(T data) =>
        new(JsonSerializer.Serialize(data, _jsonOptions), Encoding.UTF8, "application/json");

    private async Task EnsureSuccessOrThrow(
        HttpResponseMessage response, string context, CancellationToken cancellationToken)
    {
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Fuzzy-service returned {StatusCode} while {Context}: {Error}",
                response.StatusCode, context, errorContent);
            response.EnsureSuccessStatusCode();
        }
    }
}
