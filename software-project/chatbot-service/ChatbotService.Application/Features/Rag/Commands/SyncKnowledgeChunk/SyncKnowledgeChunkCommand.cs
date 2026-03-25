using MediatR;

namespace ChatbotService.Application.Features.Rag.Commands.SyncKnowledgeChunk;

/// <summary>
/// Comando M2M para sincronizar (re-vectorizar o eliminar) un knowledge chunk
/// cuando se crea/edita/elimina una entidad fuzzy.
/// </summary>
/// <param name="SourceType">Tipo de fuente: fuzzy_rule, fuzzy_system, fuzzy_variable, fuzzy_term</param>
/// <param name="SourceId">ID de la entidad original en la colección del fuzzy-service.</param>
/// <param name="Action">Acción: upsert o delete.</param>
public record SyncKnowledgeChunkCommand(string SourceType, string SourceId, string Action) : IRequest<bool>;
