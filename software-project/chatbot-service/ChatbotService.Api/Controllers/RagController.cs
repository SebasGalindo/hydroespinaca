using ChatbotService.Application.DTOs;
using ChatbotService.Application.Features.Rag.Commands.ReindexAll;
using ChatbotService.Application.Features.Rag.Commands.SyncKnowledgeChunk;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChatbotService.Api.Controllers;

/// <summary>
/// Controlador para operaciones de RAG (vectorización reactiva y re-indexación).
/// Endpoints internos M2M (machine-to-machine).
/// </summary>
[ApiController]
[Route("api/rag")]
[Authorize(Policy = "RagManage")]
public class RagController(IMediator mediator, ILogger<RagController> logger) : ControllerBase
{
    /// <summary>
    /// Sincroniza un knowledge chunk (re-vectoriza o elimina) en respuesta a
    /// cambios en entidades del fuzzy-service.
    /// </summary>
    /// <param name="request">Datos de la entidad modificada.</param>
    /// <param name="ct">Token de cancelación.</param>
    [HttpPost("sync")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SyncKnowledgeChunk([FromBody] SyncKnowledgeRequest request, CancellationToken ct)
    {
        logger.LogInformation("Recibida notificación de sync: {Action} {SourceType}/{SourceId}",
            request.Action, request.SourceType, request.SourceId);

        var command = new SyncKnowledgeChunkCommand(request.SourceType, request.SourceId, request.Action);
        var result = await mediator.Send(command, ct);

        return result ? Ok(new { success = true }) : BadRequest(new { success = false, message = "No se pudo sincronizar el chunk." });
    }

    /// <summary>
    /// Re-vectoriza toda la base de conocimientos (uso administrativo/manual).
    /// </summary>
    /// <param name="ct">Token de cancelación.</param>
    [HttpPost("reindex")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ReindexAll(CancellationToken ct)
    {
        logger.LogInformation("Iniciando re-indexación completa (manual)");

        var totalIndexed = await mediator.Send(new ReindexAllCommand(), ct);

        return Ok(new { success = true, totalChunksIndexed = totalIndexed });
    }
}
