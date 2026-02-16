using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using BffService.Application.Interfaces;
using BffService.Domain.Interfaces;
using BffService.Domain.DTOs.Fuzzy;
using BffService.Api.Helpers;

namespace BffService.Api.Controllers;

/// <summary>
/// Controller for Fuzzy Systems management.
/// Proxies requests to fuzzy-service with session-based authentication.
/// Provides an orchestrated detail endpoint that aggregates system + variables + terms + rules.
/// </summary>
[ApiController]
[Route("fuzzy")]
[AllowAnonymous] // Session is validated manually via BaseAuthenticatedController
public class FuzzyController : BaseAuthenticatedController
{
    private readonly IFuzzyServiceClient _fuzzyServiceClient;

    public FuzzyController(
        IFuzzyServiceClient fuzzyServiceClient,
        ISessionTokenService sessionTokenService,
        IConfiguration configuration,
        ILogger<FuzzyController> logger)
        : base(sessionTokenService, configuration, logger)
    {
        _fuzzyServiceClient = fuzzyServiceClient;
    }

    // ──────────────────────────────────────────────
    //  Fuzzy Systems CRUD
    // ──────────────────────────────────────────────

    /// <summary>
    /// Lists all fuzzy systems
    /// </summary>
    [HttpGet("systems")]
    [ProducesResponseType(typeof(List<FuzzySystemDto>), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> GetSystems(CancellationToken cancellationToken)
    {
        try
        {
            var session = await ValidateSessionAsync(cancellationToken);
            var result = await _fuzzyServiceClient.GetSystemsAsync(
                session.AccessToken, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return ControllerExceptionHandler.HandleException(
                ex, Logger, "getting fuzzy systems");
        }
    }

    /// <summary>
    /// Gets a single fuzzy system by ID
    /// </summary>
    [HttpGet("systems/{id}")]
    [ProducesResponseType(typeof(FuzzySystemDto), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> GetSystemById(string id, CancellationToken cancellationToken)
    {
        try
        {
            var session = await ValidateSessionAsync(cancellationToken);
            var result = await _fuzzyServiceClient.GetSystemByIdAsync(
                session.AccessToken, id, cancellationToken);

            if (result is null)
                return NotFound(new { message = "Sistema difuso no encontrado" });

            return Ok(result);
        }
        catch (Exception ex)
        {
            return ControllerExceptionHandler.HandleException(
                ex, Logger, "getting fuzzy system by id", id);
        }
    }

    /// <summary>
    /// Gets a detailed view of a fuzzy system with all variables, terms and rules.
    /// BFF orchestrates multiple calls to fuzzy-service.
    /// </summary>
    [HttpGet("systems/{id}/detail")]
    [ProducesResponseType(typeof(FuzzySystemDetailDto), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> GetSystemDetail(string id, CancellationToken cancellationToken)
    {
        try
        {
            var session = await ValidateSessionAsync(cancellationToken);

            // 1. Get the system
            var system = await _fuzzyServiceClient.GetSystemByIdAsync(
                session.AccessToken, id, cancellationToken);

            if (system is null)
                return NotFound(new { message = "Sistema difuso no encontrado" });

            // 2. Get variables for this system
            var variables = await _fuzzyServiceClient.GetVariablesBySystemAsync(
                session.AccessToken, id, cancellationToken);

            // 3. Get terms for each variable (parallel)
            var allTerms = new List<FuzzyTermDto>();
            var termTasks = variables
                .Where(v => v.Id != null)
                .Select(async v =>
                {
                    var terms = await _fuzzyServiceClient.GetTermsByVariableAsync(
                        session.AccessToken, v.Id!, cancellationToken);
                    return terms;
                })
                .ToList();

            var termResults = await Task.WhenAll(termTasks);
            foreach (var terms in termResults)
                allTerms.AddRange(terms);

            // 4. Get rules for this system
            var rules = await _fuzzyServiceClient.GetRulesBySystemAsync(
                session.AccessToken, id, cancellationToken);

            var detail = new FuzzySystemDetailDto
            {
                System = system,
                Variables = variables,
                Terms = allTerms,
                Rules = rules
            };

            return Ok(detail);
        }
        catch (Exception ex)
        {
            return ControllerExceptionHandler.HandleException(
                ex, Logger, "getting fuzzy system detail", id);
        }
    }

    /// <summary>
    /// Deletes a fuzzy system by ID
    /// </summary>
    [HttpDelete("systems/{id}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> DeleteSystem(string id, CancellationToken cancellationToken)
    {
        try
        {
            var session = await ValidateSessionAsync(cancellationToken);
            await _fuzzyServiceClient.DeleteSystemAsync(
                session.AccessToken, id, cancellationToken);
            return NoContent();
        }
        catch (Exception ex)
        {
            return ControllerExceptionHandler.HandleException(
                ex, Logger, "deleting fuzzy system", id);
        }
    }

    // ──────────────────────────────────────────────
    //  Advanced Operations
    // ──────────────────────────────────────────────

    /// <summary>
    /// Activates a fuzzy system exclusively (deactivates all others)
    /// </summary>
    [HttpPost("systems/{id}/activate")]
    [ProducesResponseType(typeof(FuzzySystemDto), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    [ProducesResponseType(422)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> ActivateSystem(string id, CancellationToken cancellationToken)
    {
        try
        {
            var session = await ValidateSessionAsync(cancellationToken);
            var result = await _fuzzyServiceClient.ActivateSystemAsync(
                session.AccessToken, id, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return ControllerExceptionHandler.HandleException(
                ex, Logger, "activating fuzzy system", id);
        }
    }

    /// <summary>
    /// Deep-clones a fuzzy system with all its variables, terms and rules
    /// </summary>
    [HttpPost("systems/{id}/clone")]
    [ProducesResponseType(typeof(FuzzySystemDto), 201)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    [ProducesResponseType(409)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> CloneSystem(
        string id,
        [FromBody] CloneFuzzySystemRequest? request,
        CancellationToken cancellationToken)
    {
        try
        {
            var session = await ValidateSessionAsync(cancellationToken);
            var result = await _fuzzyServiceClient.CloneSystemAsync(
                session.AccessToken, id, request, cancellationToken);
            return StatusCode(201, result);
        }
        catch (Exception ex)
        {
            return ControllerExceptionHandler.HandleException(
                ex, Logger, "cloning fuzzy system", id);
        }
    }

    /// <summary>
    /// Exports a fuzzy system to a portable JSON format
    /// </summary>
    [HttpGet("systems/{id}/export")]
    [ProducesResponseType(200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> ExportSystem(string id, CancellationToken cancellationToken)
    {
        try
        {
            var session = await ValidateSessionAsync(cancellationToken);
            var result = await _fuzzyServiceClient.ExportSystemAsync(
                session.AccessToken, id, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return ControllerExceptionHandler.HandleException(
                ex, Logger, "exporting fuzzy system", id);
        }
    }

    /// <summary>
    /// Imports a fuzzy system from a portable JSON format
    /// </summary>
    [HttpPost("systems/import")]
    [ProducesResponseType(typeof(FuzzySystemDto), 201)]
    [ProducesResponseType(401)]
    [ProducesResponseType(409)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> ImportSystem(
        [FromBody] Dictionary<string, object?> exportData,
        CancellationToken cancellationToken)
    {
        try
        {
            var session = await ValidateSessionAsync(cancellationToken);
            var result = await _fuzzyServiceClient.ImportSystemAsync(
                session.AccessToken, exportData, cancellationToken);
            return StatusCode(201, result);
        }
        catch (Exception ex)
        {
            return ControllerExceptionHandler.HandleException(
                ex, Logger, "importing fuzzy system");
        }
    }

    /// <summary>
    /// Simulates a fuzzy evaluation without persisting results
    /// </summary>
    [HttpPost("systems/{id}/simulate")]
    [ProducesResponseType(typeof(SimulateFuzzySystemResponse), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    [ProducesResponseType(422)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> SimulateSystem(
        string id,
        [FromBody] SimulateFuzzySystemRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var session = await ValidateSessionAsync(cancellationToken);
            var result = await _fuzzyServiceClient.SimulateSystemAsync(
                session.AccessToken, id, request, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return ControllerExceptionHandler.HandleException(
                ex, Logger, "simulating fuzzy system", id);
        }
    }
}
