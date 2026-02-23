using System.Text.Json;

namespace ChatbotService.Domain.Interfaces;

/// <summary>
/// Cliente HTTP para consultar datos del fuzzy-service.
/// Reemplaza las queries directas a MongoDB para respetar las fronteras de microservicios.
/// </summary>
public interface IFuzzyServiceClient
{
    /// <summary>
    /// Obtiene las evaluaciones fuzzy recientes (formateado para el LLM).
    /// Consume: GET /api/fuzzy-evaluations/recent?hours={hours}.
    /// </summary>
    Task<string> GetRecentEvaluationsSummaryAsync(int lastHours, CancellationToken ct);

    /// <summary>
    /// Obtiene una entidad fuzzy por su tipo e ID.
    /// Consume: GET /api/fuzzy-{type}s/{id}.
    /// </summary>
    /// <param name="sourceType">Tipo: fuzzy_rule, fuzzy_system, fuzzy_variable, fuzzy_term.</param>
    /// <param name="sourceId">ID de la entidad.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>El JSON de la entidad, o null si no existe.</returns>
    Task<JsonElement?> GetEntityByIdAsync(string sourceType, string sourceId, CancellationToken ct);

    /// <summary>
    /// Lista todas las entidades de un tipo dado (para re-indexación masiva).
    /// Consume: GET /api/fuzzy-{type}s.
    /// </summary>
    /// <param name="sourceType">Tipo: fuzzy_rule, fuzzy_system, fuzzy_variable, fuzzy_term.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>Lista de entidades como JsonElement.</returns>
    Task<List<JsonElement>> GetAllEntitiesByTypeAsync(string sourceType, CancellationToken ct);
}
