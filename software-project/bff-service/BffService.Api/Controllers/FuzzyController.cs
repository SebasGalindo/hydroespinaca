using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using BffService.Application.Interfaces;
using BffService.Domain.Interfaces;
using BffService.Domain.DTOs.Fuzzy;
using BffService.Domain.DTOs.Chatbot;
using BffService.Api.Helpers;

namespace BffService.Api.Controllers;

/// <summary>
/// Controller for Fuzzy Systems management.
/// Proxies requests to fuzzy-service with session-based authentication.
/// Provides an orchestrated detail endpoint that aggregates system + variables + terms + rules.
/// After mutations (create/update/delete), fires a best-effort sync to chatbot-service
/// for reactive re-vectorization of the affected knowledge chunk.
/// </summary>
[ApiController]
[Route("fuzzy")]
[AllowAnonymous] // Session is validated manually via BaseAuthenticatedController
public class FuzzyController : BaseAuthenticatedController
{
    private readonly IFuzzyServiceClient _fuzzyServiceClient;
    private readonly IChatbotServiceClient _chatbotClient;

    public FuzzyController(
        IFuzzyServiceClient fuzzyServiceClient,
        IChatbotServiceClient chatbotClient,
        ISessionTokenService sessionTokenService,
        IConfiguration configuration,
        ILogger<FuzzyController> logger)
        : base(sessionTokenService, configuration, logger)
    {
        _fuzzyServiceClient = fuzzyServiceClient;
        _chatbotClient = chatbotClient;
    }

    /// <summary>
    /// Fires a best-effort knowledge sync to chatbot-service.
    /// Does not block the response — errors are logged and swallowed.
    /// </summary>
    private void FireKnowledgeSync(string accessToken, string sourceType, string sourceId, string action)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await _chatbotClient.SyncKnowledgeAsync(accessToken, new SyncKnowledgeRequest
                {
                    SourceType = sourceType,
                    SourceId = sourceId,
                    Action = action
                });
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex,
                    "Best-effort knowledge sync failed for {SourceType}/{SourceId} ({Action})",
                    sourceType, sourceId, action);
            }
        });
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
    /// Creates a new fuzzy system
    /// </summary>
    [HttpPost("systems")]
    [ProducesResponseType(typeof(FuzzySystemDto), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(409)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> CreateSystem(
        [FromBody] CreateFuzzySystemRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var session = await ValidateSessionAsync(cancellationToken);
            var result = await _fuzzyServiceClient.CreateSystemAsync(
                session.AccessToken, request, cancellationToken);

            // Reactive vectorization: sync new system to chatbot knowledge base
            if (result.Id != null)
                FireKnowledgeSync(session.AccessToken, "fuzzy_system", result.Id, "upsert");

            return StatusCode(201, result);
        }
        catch (Exception ex)
        {
            return ControllerExceptionHandler.HandleException(
                ex, Logger, "creating fuzzy system");
        }
    }

    /// <summary>
    /// Updates an existing fuzzy system
    /// </summary>
    [HttpPut("systems/{id}")]
    [ProducesResponseType(typeof(FuzzySystemDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> UpdateSystem(
        string id,
        [FromBody] UpdateFuzzySystemRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var session = await ValidateSessionAsync(cancellationToken);
            var result = await _fuzzyServiceClient.UpdateSystemAsync(
                session.AccessToken, id, request, cancellationToken);

            // Reactive vectorization: sync updated system
            FireKnowledgeSync(session.AccessToken, "fuzzy_system", id, "upsert");

            return Ok(result);
        }
        catch (Exception ex)
        {
            return ControllerExceptionHandler.HandleException(
                ex, Logger, "updating fuzzy system", id);
        }
    }

    /// <summary>
    /// Updates the status of a fuzzy system (active/inactive/draft)
    /// </summary>
    [HttpPatch("systems/{id}/status")]
    [ProducesResponseType(typeof(FuzzySystemDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> UpdateSystemStatus(
        string id,
        [FromBody] UpdateFuzzySystemStatusRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var session = await ValidateSessionAsync(cancellationToken);
            var result = await _fuzzyServiceClient.UpdateSystemStatusAsync(
                session.AccessToken, id, request, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return ControllerExceptionHandler.HandleException(
                ex, Logger, "updating fuzzy system status", id);
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

            // Reactive vectorization: remove deleted system from knowledge base
            FireKnowledgeSync(session.AccessToken, "fuzzy_system", id, "delete");

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

    // ──────────────────────────────────────────────
    //  Fuzzy Variables CRUD
    // ──────────────────────────────────────────────

    /// <summary>
    /// Gets all variables for a fuzzy system
    /// </summary>
    [HttpGet("systems/{systemId}/variables")]
    [ProducesResponseType(typeof(List<FuzzyVariableDto>), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> GetVariablesBySystem(
        string systemId, CancellationToken cancellationToken)
    {
        try
        {
            var session = await ValidateSessionAsync(cancellationToken);
            var result = await _fuzzyServiceClient.GetVariablesBySystemAsync(
                session.AccessToken, systemId, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return ControllerExceptionHandler.HandleException(
                ex, Logger, "getting variables for system", systemId);
        }
    }

    /// <summary>
    /// Creates a new fuzzy variable
    /// </summary>
    [HttpPost("variables")]
    [ProducesResponseType(typeof(FuzzyVariableDto), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> CreateVariable(
        [FromBody] CreateFuzzyVariableRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var session = await ValidateSessionAsync(cancellationToken);
            var result = await _fuzzyServiceClient.CreateVariableAsync(
                session.AccessToken, request, cancellationToken);

            // Reactive vectorization: sync new variable to chatbot knowledge base
            if (result.Id != null)
                FireKnowledgeSync(session.AccessToken, "fuzzy_variable", result.Id, "upsert");

            return StatusCode(201, result);
        }
        catch (Exception ex)
        {
            return ControllerExceptionHandler.HandleException(
                ex, Logger, "creating fuzzy variable");
        }
    }

    /// <summary>
    /// Updates an existing fuzzy variable
    /// </summary>
    [HttpPut("variables/{id}")]
    [ProducesResponseType(typeof(FuzzyVariableDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> UpdateVariable(
        string id,
        [FromBody] UpdateFuzzyVariableRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var session = await ValidateSessionAsync(cancellationToken);
            var result = await _fuzzyServiceClient.UpdateVariableAsync(
                session.AccessToken, id, request, cancellationToken);

            // Reactive vectorization: sync updated variable
            FireKnowledgeSync(session.AccessToken, "fuzzy_variable", id, "upsert");

            return Ok(result);
        }
        catch (Exception ex)
        {
            return ControllerExceptionHandler.HandleException(
                ex, Logger, "updating fuzzy variable", id);
        }
    }

    /// <summary>
    /// Deletes a fuzzy variable by ID
    /// </summary>
    [HttpDelete("variables/{id}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> DeleteVariable(
        string id, CancellationToken cancellationToken)
    {
        try
        {
            var session = await ValidateSessionAsync(cancellationToken);
            await _fuzzyServiceClient.DeleteVariableAsync(
                session.AccessToken, id, cancellationToken);

            // Reactive vectorization: remove deleted variable from knowledge base
            FireKnowledgeSync(session.AccessToken, "fuzzy_variable", id, "delete");

            return NoContent();
        }
        catch (Exception ex)
        {
            return ControllerExceptionHandler.HandleException(
                ex, Logger, "deleting fuzzy variable", id);
        }
    }

    // ──────────────────────────────────────────────
    //  Fuzzy Terms CRUD
    // ──────────────────────────────────────────────

    /// <summary>
    /// Gets all terms for a fuzzy variable
    /// </summary>
    [HttpGet("variables/{variableId}/terms")]
    [ProducesResponseType(typeof(List<FuzzyTermDto>), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> GetTermsByVariable(
        string variableId, CancellationToken cancellationToken)
    {
        try
        {
            var session = await ValidateSessionAsync(cancellationToken);
            var result = await _fuzzyServiceClient.GetTermsByVariableAsync(
                session.AccessToken, variableId, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return ControllerExceptionHandler.HandleException(
                ex, Logger, "getting terms for variable", variableId);
        }
    }

    /// <summary>
    /// Creates a new fuzzy term
    /// </summary>
    [HttpPost("terms")]
    [ProducesResponseType(typeof(FuzzyTermDto), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> CreateTerm(
        [FromBody] CreateFuzzyTermRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var session = await ValidateSessionAsync(cancellationToken);
            var result = await _fuzzyServiceClient.CreateTermAsync(
                session.AccessToken, request, cancellationToken);

            // Reactive vectorization: sync new term to chatbot knowledge base
            if (result.Id != null)
                FireKnowledgeSync(session.AccessToken, "fuzzy_term", result.Id, "upsert");

            return StatusCode(201, result);
        }
        catch (Exception ex)
        {
            return ControllerExceptionHandler.HandleException(
                ex, Logger, "creating fuzzy term");
        }
    }

    /// <summary>
    /// Updates an existing fuzzy term
    /// </summary>
    [HttpPut("terms/{id}")]
    [ProducesResponseType(typeof(FuzzyTermDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> UpdateTerm(
        string id,
        [FromBody] UpdateFuzzyTermRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var session = await ValidateSessionAsync(cancellationToken);
            var result = await _fuzzyServiceClient.UpdateTermAsync(
                session.AccessToken, id, request, cancellationToken);

            // Reactive vectorization: sync updated term
            FireKnowledgeSync(session.AccessToken, "fuzzy_term", id, "upsert");

            return Ok(result);
        }
        catch (Exception ex)
        {
            return ControllerExceptionHandler.HandleException(
                ex, Logger, "updating fuzzy term", id);
        }
    }

    /// <summary>
    /// Deletes a fuzzy term by ID
    /// </summary>
    [HttpDelete("terms/{id}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> DeleteTerm(
        string id, CancellationToken cancellationToken)
    {
        try
        {
            var session = await ValidateSessionAsync(cancellationToken);
            await _fuzzyServiceClient.DeleteTermAsync(
                session.AccessToken, id, cancellationToken);

            // Reactive vectorization: remove deleted term from knowledge base
            FireKnowledgeSync(session.AccessToken, "fuzzy_term", id, "delete");

            return NoContent();
        }
        catch (Exception ex)
        {
            return ControllerExceptionHandler.HandleException(
                ex, Logger, "deleting fuzzy term", id);
        }
    }

    // ──────────────────────────────────────────────
    //  Fuzzy Rules CRUD
    // ──────────────────────────────────────────────

    /// <summary>
    /// Gets all rules for a fuzzy system
    /// </summary>
    [HttpGet("systems/{systemId}/rules")]
    [ProducesResponseType(typeof(List<FuzzyRuleDto>), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> GetRulesBySystem(
        string systemId, CancellationToken cancellationToken)
    {
        try
        {
            var session = await ValidateSessionAsync(cancellationToken);
            var result = await _fuzzyServiceClient.GetRulesBySystemAsync(
                session.AccessToken, systemId, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return ControllerExceptionHandler.HandleException(
                ex, Logger, "getting rules for system", systemId);
        }
    }

    /// <summary>
    /// Creates a new fuzzy rule
    /// </summary>
    [HttpPost("rules")]
    [ProducesResponseType(typeof(FuzzyRuleDto), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> CreateRule(
        [FromBody] CreateFuzzyRuleRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var session = await ValidateSessionAsync(cancellationToken);
            var result = await _fuzzyServiceClient.CreateRuleAsync(
                session.AccessToken, request, cancellationToken);

            // Reactive vectorization: sync new rule to chatbot knowledge base
            if (result.Id != null)
                FireKnowledgeSync(session.AccessToken, "fuzzy_rule", result.Id, "upsert");

            return StatusCode(201, result);
        }
        catch (Exception ex)
        {
            return ControllerExceptionHandler.HandleException(
                ex, Logger, "creating fuzzy rule");
        }
    }

    /// <summary>
    /// Updates an existing fuzzy rule
    /// </summary>
    [HttpPut("rules/{id}")]
    [ProducesResponseType(typeof(FuzzyRuleDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> UpdateRule(
        string id,
        [FromBody] UpdateFuzzyRuleRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var session = await ValidateSessionAsync(cancellationToken);
            var result = await _fuzzyServiceClient.UpdateRuleAsync(
                session.AccessToken, id, request, cancellationToken);

            // Reactive vectorization: sync updated rule
            FireKnowledgeSync(session.AccessToken, "fuzzy_rule", id, "upsert");

            return Ok(result);
        }
        catch (Exception ex)
        {
            return ControllerExceptionHandler.HandleException(
                ex, Logger, "updating fuzzy rule", id);
        }
    }

    /// <summary>
    /// Deletes a fuzzy rule by ID
    /// </summary>
    [HttpDelete("rules/{id}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> DeleteRule(
        string id, CancellationToken cancellationToken)
    {
        try
        {
            var session = await ValidateSessionAsync(cancellationToken);
            await _fuzzyServiceClient.DeleteRuleAsync(
                session.AccessToken, id, cancellationToken);

            // Reactive vectorization: remove deleted rule from knowledge base
            FireKnowledgeSync(session.AccessToken, "fuzzy_rule", id, "delete");

            return NoContent();
        }
        catch (Exception ex)
        {
            return ControllerExceptionHandler.HandleException(
                ex, Logger, "deleting fuzzy rule", id);
        }
    }

    // ──────────────────────────────────────────────
    //  Fuzzy Evaluations (RF-F07)
    // ──────────────────────────────────────────────

    /// <summary>
    /// Lists persisted fuzzy evaluations with optional filters and pagination.
    /// </summary>
    [HttpGet("evaluations")]
    [ProducesResponseType(typeof(FuzzyEvaluationsListResponseDto), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> GetEvaluations(
        [FromQuery(Name = "systemId")] string? systemId,
        [FromQuery(Name = "startDate")] DateTime? startDate,
        [FromQuery(Name = "endDate")] DateTime? endDate,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string sortBy = "timestamp",
        [FromQuery] string sortOrder = "desc",
        CancellationToken cancellationToken = default)
    {
        try
        {
            var session = await ValidateSessionAsync(cancellationToken);
            var result = await _fuzzyServiceClient.GetEvaluationsAsync(
                session.AccessToken, systemId, startDate, endDate,
                page, pageSize, sortBy, sortOrder, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return ControllerExceptionHandler.HandleException(
                ex, Logger, "getting fuzzy evaluations");
        }
    }

    /// <summary>
    /// Lists evaluations performed in the last N hours (default 24, max 168).
    /// </summary>
    [HttpGet("evaluations/recent")]
    [ProducesResponseType(typeof(FuzzyEvaluationsListResponseDto), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> GetRecentEvaluations(
        [FromQuery] int hours = 24,
        [FromQuery(Name = "systemId")] string? systemId = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var session = await ValidateSessionAsync(cancellationToken);
            var result = await _fuzzyServiceClient.GetRecentEvaluationsAsync(
                session.AccessToken, hours, systemId, page, pageSize, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return ControllerExceptionHandler.HandleException(
                ex, Logger, "getting recent fuzzy evaluations");
        }
    }

    /// <summary>
    /// Lists evaluations for a specific fuzzy system, with optional date range.
    /// </summary>
    [HttpGet("systems/{systemId}/evaluations")]
    [ProducesResponseType(typeof(FuzzyEvaluationsListResponseDto), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> GetEvaluationsBySystem(
        string systemId,
        [FromQuery(Name = "startDate")] DateTime? startDate = null,
        [FromQuery(Name = "endDate")] DateTime? endDate = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string sortOrder = "desc",
        CancellationToken cancellationToken = default)
    {
        try
        {
            var session = await ValidateSessionAsync(cancellationToken);
            var result = await _fuzzyServiceClient.GetEvaluationsBySystemAsync(
                session.AccessToken, systemId, startDate, endDate,
                page, pageSize, sortOrder, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return ControllerExceptionHandler.HandleException(
                ex, Logger, "getting fuzzy evaluations by system", systemId);
        }
    }

    /// <summary>
    /// Returns aggregate statistics for fuzzy evaluations over the last N days.
    /// </summary>
    [HttpGet("evaluations/stats")]
    [ProducesResponseType(typeof(FuzzyEvaluationStatsDto), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> GetEvaluationStats(
        [FromQuery(Name = "systemId")] string? systemId = null,
        [FromQuery] int days = 7,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var session = await ValidateSessionAsync(cancellationToken);
            var result = await _fuzzyServiceClient.GetEvaluationStatsAsync(
                session.AccessToken, systemId, days, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return ControllerExceptionHandler.HandleException(
                ex, Logger, "getting fuzzy evaluation stats");
        }
    }
}
