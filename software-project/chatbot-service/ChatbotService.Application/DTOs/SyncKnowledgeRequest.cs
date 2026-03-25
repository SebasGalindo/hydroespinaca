namespace ChatbotService.Application.DTOs;

/// <summary>
/// Request para sincronización de un knowledge chunk (notificación M2M).
/// </summary>
public class SyncKnowledgeRequest
{
    /// <summary>
    /// Tipo de fuente: fuzzy_rule, fuzzy_system, fuzzy_variable, fuzzy term, system_manual.
    /// </summary>
    public required string SourceType { get; set; }

    /// <summary>
    /// ID de la entidad original.
    /// </summary>
    public required string SourceId { get; set; }

    /// <summary>
    /// Acción: upsert o delete.
    /// </summary>
    public required string Action { get; set; }
}
