using BffService.Application.Interfaces;
using BffService.Domain.DTOs.Bi;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace BffService.Infrastructure.Services;

/// <summary>
/// HTTP client for communicating with the bi-service microservice.
/// Follows the same typed HttpClient pattern as FuzzyServiceClient.
/// Forwards the user's access token for authorization on bi-service.
/// </summary>
public class BiServiceClient : IBiServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<BiServiceClient> _logger;
    private readonly string _baseUrl;
    private readonly JsonSerializerOptions _jsonOptions;

    public BiServiceClient(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<BiServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;

        _baseUrl = configuration["Services:BiService:Url"] ?? "http://bi-service:8080";

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    public async Task<CostConfigVersionDto?> GetCurrentCostConfigAsync(
        string accessToken, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Fetching current cost config from bi-service");

            var request = CreateRequest(HttpMethod.Get, "/api/bi/cost-config/current", accessToken);
            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogInformation("No active cost config found in bi-service");
                return null;
            }

            await EnsureSuccessOrThrow(response, "getting current cost config", cancellationToken);

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<CostConfigVersionDto>(content, _jsonOptions);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error fetching current cost config from bi-service");
            throw;
        }
        catch (Exception ex) when (ex is not HttpRequestException)
        {
            _logger.LogError(ex, "Unexpected error fetching current cost config from bi-service");
            throw;
        }
    }

    public async Task<List<CostConfigVersionDto>> GetCostConfigVersionsAsync(
        string accessToken, DateTime? from = null, DateTime? to = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Fetching cost config versions from bi-service");

            var queryParams = new List<string>();
            if (from.HasValue) queryParams.Add($"from={from.Value:O}");
            if (to.HasValue) queryParams.Add($"to={to.Value:O}");
            var queryString = queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : "";

            var request = CreateRequest(HttpMethod.Get,
                $"/api/bi/cost-config/versions{queryString}", accessToken);
            var response = await _httpClient.SendAsync(request, cancellationToken);

            await EnsureSuccessOrThrow(response, "getting cost config versions", cancellationToken);

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<List<CostConfigVersionDto>>(content, _jsonOptions)
                ?? new List<CostConfigVersionDto>();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error fetching cost config versions from bi-service");
            throw;
        }
        catch (Exception ex) when (ex is not HttpRequestException)
        {
            _logger.LogError(ex, "Unexpected error fetching cost config versions from bi-service");
            throw;
        }
    }

    public async Task<CostConfigVersionDto> CreateCostConfigVersionAsync(
        string accessToken, CreateCostConfigVersionRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Creating cost config version in bi-service");

            var httpRequest = CreateRequest(HttpMethod.Post,
                "/api/bi/cost-config/versions", accessToken);
            httpRequest.Content = new StringContent(
                JsonSerializer.Serialize(request, _jsonOptions),
                Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(httpRequest, cancellationToken);

            await EnsureSuccessOrThrow(response, "creating cost config version", cancellationToken);

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<CostConfigVersionDto>(content, _jsonOptions)
                ?? throw new InvalidOperationException("bi-service returned null for created cost config version");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error creating cost config version in bi-service");
            throw;
        }
        catch (Exception ex) when (ex is not HttpRequestException and not InvalidOperationException)
        {
            _logger.LogError(ex, "Unexpected error creating cost config version in bi-service");
            throw;
        }
    }

    public async Task<CostConfigVersionDto> UpdateCostConfigVersionAsync(
        string accessToken, string id, UpdateCostConfigVersionRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Updating cost config version {Id} in bi-service", id);

            var httpRequest = CreateRequest(HttpMethod.Put,
                $"/api/bi/cost-config/versions/{id}", accessToken);
            httpRequest.Content = new StringContent(
                JsonSerializer.Serialize(request, _jsonOptions),
                Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(httpRequest, cancellationToken);

            await EnsureSuccessOrThrow(response, "updating cost config version", cancellationToken);

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<CostConfigVersionDto>(content, _jsonOptions)
                ?? throw new InvalidOperationException("bi-service returned null for updated cost config version");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error updating cost config version in bi-service");
            throw;
        }
        catch (Exception ex) when (ex is not HttpRequestException and not InvalidOperationException)
        {
            _logger.LogError(ex, "Unexpected error updating cost config version in bi-service");
            throw;
        }
    }

    public async Task DeleteCostConfigVersionAsync(
        string accessToken, string id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Deleting cost config version {Id} from bi-service", id);

            var request = CreateRequest(HttpMethod.Delete,
                $"/api/bi/cost-config/versions/{id}", accessToken);
            var response = await _httpClient.SendAsync(request, cancellationToken);

            await EnsureSuccessOrThrow(response, "deleting cost config version", cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error deleting cost config version from bi-service");
            throw;
        }
        catch (Exception ex) when (ex is not HttpRequestException)
        {
            _logger.LogError(ex, "Unexpected error deleting cost config version from bi-service");
            throw;
        }
    }

    public async Task<ManualConsumptionEntryDto> CreateConsumptionEntryAsync(
        string accessToken, CreateManualConsumptionEntryRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Creating consumption entry in bi-service");

            var httpRequest = CreateRequest(HttpMethod.Post,
                "/api/bi/consumption-entries", accessToken);
            httpRequest.Content = new StringContent(
                JsonSerializer.Serialize(request, _jsonOptions),
                Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(httpRequest, cancellationToken);

            await EnsureSuccessOrThrow(response, "creating consumption entry", cancellationToken);

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<ManualConsumptionEntryDto>(content, _jsonOptions)
                ?? throw new InvalidOperationException("bi-service returned null for created consumption entry");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error creating consumption entry in bi-service");
            throw;
        }
        catch (Exception ex) when (ex is not HttpRequestException and not InvalidOperationException)
        {
            _logger.LogError(ex, "Unexpected error creating consumption entry in bi-service");
            throw;
        }
    }

    public async Task<List<ManualConsumptionEntryDto>> GetConsumptionEntriesAsync(
        string accessToken, DateTime from, DateTime to, string? type = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Fetching consumption entries from bi-service: {From} to {To}", from, to);

            var queryParams = new List<string>
            {
                $"from={from:O}",
                $"to={to:O}"
            };
            // Note: type is passed as-is (int/enum value) if provided
            if (!string.IsNullOrEmpty(type)) queryParams.Add($"type={type}");

            var queryString = "?" + string.Join("&", queryParams);
            var request = CreateRequest(HttpMethod.Get,
                $"/api/bi/consumption-entries{queryString}", accessToken);
            var response = await _httpClient.SendAsync(request, cancellationToken);

            await EnsureSuccessOrThrow(response, "getting consumption entries", cancellationToken);

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<List<ManualConsumptionEntryDto>>(content, _jsonOptions)
                ?? new List<ManualConsumptionEntryDto>();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error fetching consumption entries from bi-service");
            throw;
        }
        catch (Exception ex) when (ex is not HttpRequestException)
        {
            _logger.LogError(ex, "Unexpected error fetching consumption entries from bi-service");
            throw;
        }
    }

    public async Task<BiSummaryDto> GetConsumptionSummaryAsync(
        string accessToken, DateTime from, DateTime to,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Fetching consumption summary from bi-service: {From} to {To}", from, to);

            var request = CreateRequest(HttpMethod.Get,
                $"/api/bi/consumption-entries/summary?from={from:O}&to={to:O}", accessToken);
            var response = await _httpClient.SendAsync(request, cancellationToken);

            await EnsureSuccessOrThrow(response, "getting consumption summary", cancellationToken);

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<BiSummaryDto>(content, _jsonOptions)
                ?? throw new InvalidOperationException("bi-service returned null for consumption summary");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error fetching consumption summary from bi-service");
            throw;
        }
        catch (Exception ex) when (ex is not HttpRequestException and not InvalidOperationException)
        {
            _logger.LogError(ex, "Unexpected error fetching consumption summary from bi-service");
            throw;
        }
    }

    #region Delete Consumption Entry

    public async Task DeleteConsumptionEntryAsync(
        string accessToken, string id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Deleting consumption entry {Id} from bi-service", id);

            var request = CreateRequest(HttpMethod.Delete,
                $"/api/bi/consumption-entries/{id}", accessToken);
            var response = await _httpClient.SendAsync(request, cancellationToken);

            await EnsureSuccessOrThrow(response, "deleting consumption entry", cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error deleting consumption entry from bi-service");
            throw;
        }
        catch (Exception ex) when (ex is not HttpRequestException)
        {
            _logger.LogError(ex, "Unexpected error deleting consumption entry from bi-service");
            throw;
        }
    }

    #endregion

    #region Production Records

    public async Task<ProductionRecordDto> CreateProductionRecordAsync(
        string accessToken, CreateProductionRecordRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Creating production record in bi-service");

            var httpRequest = CreateRequest(HttpMethod.Post,
                "/api/bi/production-records", accessToken);
            httpRequest.Content = new StringContent(
                JsonSerializer.Serialize(request, _jsonOptions),
                Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(httpRequest, cancellationToken);

            await EnsureSuccessOrThrow(response, "creating production record", cancellationToken);

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<ProductionRecordDto>(content, _jsonOptions)
                ?? throw new InvalidOperationException("bi-service returned null for created production record");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error creating production record in bi-service");
            throw;
        }
        catch (Exception ex) when (ex is not HttpRequestException and not InvalidOperationException)
        {
            _logger.LogError(ex, "Unexpected error creating production record in bi-service");
            throw;
        }
    }

    public async Task<List<ProductionRecordDto>> GetProductionRecordsAsync(
        string accessToken, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Fetching production records from bi-service");

            var request = CreateRequest(HttpMethod.Get,
                "/api/bi/production-records", accessToken);
            var response = await _httpClient.SendAsync(request, cancellationToken);

            await EnsureSuccessOrThrow(response, "getting production records", cancellationToken);

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<List<ProductionRecordDto>>(content, _jsonOptions)
                ?? new List<ProductionRecordDto>();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error fetching production records from bi-service");
            throw;
        }
        catch (Exception ex) when (ex is not HttpRequestException)
        {
            _logger.LogError(ex, "Unexpected error fetching production records from bi-service");
            throw;
        }
    }

    public async Task<ProductionRecordDto?> GetProductionRecordByIdAsync(
        string accessToken, string id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Fetching production record {Id} from bi-service", id);

            var request = CreateRequest(HttpMethod.Get,
                $"/api/bi/production-records/{id}", accessToken);
            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogInformation("Production record {Id} not found in bi-service", id);
                return null;
            }

            await EnsureSuccessOrThrow(response, "getting production record by id", cancellationToken);

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<ProductionRecordDto>(content, _jsonOptions);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error fetching production record from bi-service");
            throw;
        }
        catch (Exception ex) when (ex is not HttpRequestException)
        {
            _logger.LogError(ex, "Unexpected error fetching production record from bi-service");
            throw;
        }
    }

    public async Task DeleteProductionRecordAsync(
        string accessToken, string id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Deleting production record {Id} from bi-service", id);

            var request = CreateRequest(HttpMethod.Delete,
                $"/api/bi/production-records/{id}", accessToken);
            var response = await _httpClient.SendAsync(request, cancellationToken);

            await EnsureSuccessOrThrow(response, "deleting production record", cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error deleting production record from bi-service");
            throw;
        }
        catch (Exception ex) when (ex is not HttpRequestException)
        {
            _logger.LogError(ex, "Unexpected error deleting production record from bi-service");
            throw;
        }
    }

    #endregion

    #region Operational Cost

    public async Task<OperationalCostResponse> CalculateOperationalCostAsync(
        string accessToken, CalculateOperationalCostServiceRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Calculating operational cost via bi-service: {From} to {To}",
                request.From, request.To);

            var httpRequest = CreateRequest(HttpMethod.Post,
                "/api/bi/operational-cost/calculate", accessToken);
            httpRequest.Content = new StringContent(
                JsonSerializer.Serialize(request, _jsonOptions),
                Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(httpRequest, cancellationToken);

            await EnsureSuccessOrThrow(response, "calculating operational cost", cancellationToken);

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<OperationalCostResponse>(content, _jsonOptions)
                ?? throw new InvalidOperationException("bi-service returned null for operational cost");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error calculating operational cost via bi-service");
            throw;
        }
        catch (Exception ex) when (ex is not HttpRequestException and not InvalidOperationException)
        {
            _logger.LogError(ex, "Unexpected error calculating operational cost via bi-service");
            throw;
        }
    }

    #endregion

    #region Profitability

    public async Task<ProfitabilityResponse> CalculateProfitabilityAsync(
        string accessToken, CalculateProfitabilityServiceRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Calculating profitability via bi-service for production {Id}",
                request.ProductionRecordId);

            var httpRequest = CreateRequest(HttpMethod.Post,
                "/api/bi/profitability/calculate", accessToken);
            httpRequest.Content = new StringContent(
                JsonSerializer.Serialize(request, _jsonOptions),
                Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(httpRequest, cancellationToken);

            await EnsureSuccessOrThrow(response, "calculating profitability", cancellationToken);

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<ProfitabilityResponse>(content, _jsonOptions)
                ?? throw new InvalidOperationException("bi-service returned null for profitability");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error calculating profitability via bi-service");
            throw;
        }
        catch (Exception ex) when (ex is not HttpRequestException and not InvalidOperationException)
        {
            _logger.LogError(ex, "Unexpected error calculating profitability via bi-service");
            throw;
        }
    }

    #endregion

    #region Private Helpers

    private HttpRequestMessage CreateRequest(HttpMethod method, string path, string accessToken)
    {
        var url = $"{_baseUrl}{path}";
        _logger.LogDebug("Bi-service request: {Method} {Url}", method, url);

        var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return request;
    }

    private async Task EnsureSuccessOrThrow(
        HttpResponseMessage response, string context, CancellationToken cancellationToken)
    {
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Bi-service returned {StatusCode} while {Context}: {Error}",
                response.StatusCode, context, errorContent);
            response.EnsureSuccessStatusCode();
        }
    }

    #endregion
}
