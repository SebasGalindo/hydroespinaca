using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using BffService.Application.Interfaces;
using BffService.Domain.Interfaces;
using BffService.Domain.DTOs.Bi;
using BffService.Api.Helpers;

namespace BffService.Api.Controllers;

/// <summary>
/// Controller for BI (Business Intelligence) operations.
/// Proxies requests to bi-service with session-based authentication.
/// Orchestrates calls between actuator-service and bi-service for cost/profitability endpoints.
/// </summary>
[ApiController]
[Route("bi")]
[AllowAnonymous] // Session is validated manually via BaseAuthenticatedController
public class BiController : BaseAuthenticatedController
{
    private readonly IBiServiceClient _biServiceClient;
    private readonly IBiOrchestrationService _orchestrationService;

    public BiController(
        IBiServiceClient biServiceClient,
        IBiOrchestrationService orchestrationService,
        ISessionTokenService sessionTokenService,
        IConfiguration configuration,
        ILogger<BiController> logger)
        : base(sessionTokenService, configuration, logger)
    {
        _biServiceClient = biServiceClient;
        _orchestrationService = orchestrationService;
    }

    // ──────────────────────────────────────────────
    //  Cost Configuration
    // ──────────────────────────────────────────────

    /// <summary>
    /// Gets the current active cost configuration
    /// </summary>
    [HttpGet("cost-config/current")]
    [ProducesResponseType(typeof(CostConfigVersionDto), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(401)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> GetCurrentCostConfig(CancellationToken cancellationToken)
    {
        try
        {
            var session = await ValidateSessionAsync(cancellationToken);
            var result = await _biServiceClient.GetCurrentCostConfigAsync(
                session.AccessToken, cancellationToken);

            if (result is null)
                return NotFound(new { message = "No hay configuración de costos activa" });

            return Ok(result);
        }
        catch (Exception ex)
        {
            return ControllerExceptionHandler.HandleException(
                ex, Logger, "getting current cost config");
        }
    }

    /// <summary>
    /// Gets cost configuration version history with optional date range filter
    /// </summary>
    [HttpGet("cost-config/versions")]
    [ProducesResponseType(typeof(List<CostConfigVersionDto>), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> GetCostConfigVersions(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken cancellationToken)
    {
        try
        {
            var session = await ValidateSessionAsync(cancellationToken);
            var result = await _biServiceClient.GetCostConfigVersionsAsync(
                session.AccessToken, from, to, cancellationToken);

            return Ok(result);
        }
        catch (Exception ex)
        {
            return ControllerExceptionHandler.HandleException(
                ex, Logger, "getting cost config versions");
        }
    }

    /// <summary>
    /// Creates a new cost configuration version
    /// </summary>
    [HttpPost("cost-config/versions")]
    [ProducesResponseType(typeof(CostConfigVersionDto), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> CreateCostConfigVersion(
        [FromBody] CreateCostConfigVersionRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var session = await ValidateSessionAsync(cancellationToken);
            var result = await _biServiceClient.CreateCostConfigVersionAsync(
                session.AccessToken, request, cancellationToken);

            return CreatedAtAction(nameof(GetCurrentCostConfig), new { }, result);
        }
        catch (Exception ex)
        {
            return ControllerExceptionHandler.HandleException(
                ex, Logger, "creating cost config version");
        }
    }

    // ──────────────────────────────────────────────
    //  Consumption Entries
    // ──────────────────────────────────────────────

    /// <summary>
    /// Creates a new manual consumption entry
    /// </summary>
    [HttpPost("consumption-entries")]
    [ProducesResponseType(typeof(ManualConsumptionEntryDto), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> CreateConsumptionEntry(
        [FromBody] CreateManualConsumptionEntryRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var session = await ValidateSessionAsync(cancellationToken);
            var result = await _biServiceClient.CreateConsumptionEntryAsync(
                session.AccessToken, request, cancellationToken);

            return CreatedAtAction(nameof(GetConsumptionEntries),
                new { from = result.Date.Date, to = result.Date.Date }, result);
        }
        catch (Exception ex)
        {
            return ControllerExceptionHandler.HandleException(
                ex, Logger, "creating consumption entry");
        }
    }

    /// <summary>
    /// Gets consumption entries filtered by date range and optional type
    /// </summary>
    [HttpGet("consumption-entries")]
    [ProducesResponseType(typeof(List<ManualConsumptionEntryDto>), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> GetConsumptionEntries(
        [FromQuery] DateTime from,
        [FromQuery] DateTime to,
        [FromQuery] string? type,
        CancellationToken cancellationToken)
    {
        try
        {
            var session = await ValidateSessionAsync(cancellationToken);
            var result = await _biServiceClient.GetConsumptionEntriesAsync(
                session.AccessToken, from, to, type, cancellationToken);

            return Ok(result);
        }
        catch (Exception ex)
        {
            return ControllerExceptionHandler.HandleException(
                ex, Logger, "getting consumption entries");
        }
    }

    /// <summary>
    /// Gets a cost summary for the given date range
    /// </summary>
    [HttpGet("consumption-entries/summary")]
    [ProducesResponseType(typeof(BiSummaryDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> GetConsumptionSummary(
        [FromQuery] DateTime from,
        [FromQuery] DateTime to,
        CancellationToken cancellationToken)
    {
        try
        {
            var session = await ValidateSessionAsync(cancellationToken);
            var result = await _biServiceClient.GetConsumptionSummaryAsync(
                session.AccessToken, from, to, cancellationToken);

            return Ok(result);
        }
        catch (Exception ex)
        {
            return ControllerExceptionHandler.HandleException(
                ex, Logger, "getting consumption summary");
        }
    }

    /// <summary>
    /// Deletes a manual consumption entry by ID
    /// </summary>
    [HttpDelete("consumption-entries/{id}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> DeleteConsumptionEntry(
        string id, CancellationToken cancellationToken)
    {
        try
        {
            var session = await ValidateSessionAsync(cancellationToken);
            await _biServiceClient.DeleteConsumptionEntryAsync(
                session.AccessToken, id, cancellationToken);

            return NoContent();
        }
        catch (Exception ex)
        {
            return ControllerExceptionHandler.HandleException(
                ex, Logger, "deleting consumption entry");
        }
    }

    // ──────────────────────────────────────────────
    //  Production Records
    // ──────────────────────────────────────────────

    /// <summary>
    /// Creates a new production record (harvest tracking)
    /// </summary>
    [HttpPost("production-records")]
    [ProducesResponseType(typeof(ProductionRecordDto), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> CreateProductionRecord(
        [FromBody] CreateProductionRecordRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var session = await ValidateSessionAsync(cancellationToken);
            var result = await _biServiceClient.CreateProductionRecordAsync(
                session.AccessToken, request, cancellationToken);

            return CreatedAtAction(nameof(GetProductionRecordById),
                new { id = result.Id }, result);
        }
        catch (Exception ex)
        {
            return ControllerExceptionHandler.HandleException(
                ex, Logger, "creating production record");
        }
    }

    /// <summary>
    /// Gets all production records
    /// </summary>
    [HttpGet("production-records")]
    [ProducesResponseType(typeof(List<ProductionRecordDto>), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> GetProductionRecords(
        CancellationToken cancellationToken)
    {
        try
        {
            var session = await ValidateSessionAsync(cancellationToken);
            var result = await _biServiceClient.GetProductionRecordsAsync(
                session.AccessToken, cancellationToken);

            return Ok(result);
        }
        catch (Exception ex)
        {
            return ControllerExceptionHandler.HandleException(
                ex, Logger, "getting production records");
        }
    }

    /// <summary>
    /// Gets a production record by ID
    /// </summary>
    [HttpGet("production-records/{id}")]
    [ProducesResponseType(typeof(ProductionRecordDto), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> GetProductionRecordById(
        string id, CancellationToken cancellationToken)
    {
        try
        {
            var session = await ValidateSessionAsync(cancellationToken);
            var result = await _biServiceClient.GetProductionRecordByIdAsync(
                session.AccessToken, id, cancellationToken);

            if (result is null)
                return NotFound(new { message = "Registro de producción no encontrado" });

            return Ok(result);
        }
        catch (Exception ex)
        {
            return ControllerExceptionHandler.HandleException(
                ex, Logger, "getting production record by id");
        }
    }

    /// <summary>
    /// Deletes a production record by ID
    /// </summary>
    [HttpDelete("production-records/{id}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> DeleteProductionRecord(
        string id, CancellationToken cancellationToken)
    {
        try
        {
            var session = await ValidateSessionAsync(cancellationToken);
            await _biServiceClient.DeleteProductionRecordAsync(
                session.AccessToken, id, cancellationToken);

            return NoContent();
        }
        catch (Exception ex)
        {
            return ControllerExceptionHandler.HandleException(
                ex, Logger, "deleting production record");
        }
    }

    // ──────────────────────────────────────────────
    //  Operational Cost (BFF orchestrated)
    // ──────────────────────────────────────────────

    /// <summary>
    /// Calculates operational cost for a date range.
    /// BFF orchestrates: fetches actuator data + analytics from actuator-service,
    /// combines PowerConsumptionWatts with durations, sends to bi-service for calculation.
    /// </summary>
    [HttpPost("operational-cost/calculate")]
    [ProducesResponseType(typeof(OperationalCostResponse), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> CalculateOperationalCost(
        [FromBody] CalculateOperationalCostBffRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var session = await ValidateSessionAsync(cancellationToken);
            var result = await _orchestrationService.CalculateOperationalCostAsync(
                request, session.AccessToken, cancellationToken);

            return Ok(result);
        }
        catch (Exception ex)
        {
            return ControllerExceptionHandler.HandleException(
                ex, Logger, "calculating operational cost");
        }
    }

    // ──────────────────────────────────────────────
    //  Profitability (BFF orchestrated)
    // ──────────────────────────────────────────────

    /// <summary>
    /// Calculates profitability for a production record.
    /// BFF orchestrates: gets production dates, fetches actuator data + analytics,
    /// combines and sends to bi-service for full profitability analysis.
    /// </summary>
    [HttpPost("profitability/calculate")]
    [ProducesResponseType(typeof(ProfitabilityResponse), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> CalculateProfitability(
        [FromBody] CalculateProfitabilityBffRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var session = await ValidateSessionAsync(cancellationToken);
            var result = await _orchestrationService.CalculateProfitabilityAsync(
                request, session.AccessToken, cancellationToken);

            return Ok(result);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("no encontrado"))
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return ControllerExceptionHandler.HandleException(
                ex, Logger, "calculating profitability");
        }
    }
}
