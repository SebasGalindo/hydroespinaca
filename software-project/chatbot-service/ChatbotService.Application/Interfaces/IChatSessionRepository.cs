using ChatbotService.Domain.Entities;

namespace ChatbotService.Application.Interfaces;

/// <summary>
/// Proporciona los CRUD básicos de <see cref="ChatSession"/> sobre la DB "HydroEspinacaDB".
/// </summary>
public interface IChatSessionRepository
{
    /// <summary>
    /// Obtiene una sesión por su identificador único.
    /// </summary>
    /// <param name="sessionId">ID de la sesión.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>La sesión encontrada o <c>null</c>.</returns>
    Task<ChatSession?> GetByIdAsync(string sessionId, CancellationToken ct);

    /// <summary>
    /// Crea una nueva sesión de chat en la base de datos.
    /// </summary>
    /// <param name="session">La sesión a crear.</param>
    /// <param name="ct">Token de cancelación.</param>
    Task CreateAsync(ChatSession session, CancellationToken ct);

    /// <summary>
    /// Consulta las sesiones según el UserId ordenándolas descendentemente por <c>UpdatedAt</c>.
    /// </summary>
    /// <param name="userId">ID del usuario autenticado.</param>
    /// <param name="skip">Registros a omitir (paginación).</param>
    /// <param name="limit">Máximo de registros a retornar.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>Lista paginada de sesiones.</returns>
    Task<List<ChatSession>> GetSessionsByUserAsync(string userId, int skip, int limit, CancellationToken ct);

    /// <summary>
    /// Agrega un mensaje a una sesión existente mediante <c>$push</c> de MongoDB.
    /// </summary>
    /// <param name="sessionId">ID de la sesión destino.</param>
    /// <param name="message">El mensaje a agregar.</param>
    /// <param name="ct">Token de cancelación.</param>
    Task AddMessageToSessionAsync(string sessionId, ChatMessage message, CancellationToken ct);

    /// <summary>
    /// Elimina (archiva) una sesión por su identificador.
    /// </summary>
    /// <param name="sessionId">ID de la sesión.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns><c>true</c> si se eliminó correctamente.</returns>
    Task<bool> DeleteAsync(string sessionId, CancellationToken ct);

    /// <summary>
    /// Actualiza el título de una sesión existente.
    /// </summary>
    /// <param name="sessionId">ID de la sesión.</param>
    /// <param name="title">Nuevo título.</param>
    /// <param name="ct">Token de cancelación.</param>
    Task UpdateTitleAsync(string sessionId, string title, CancellationToken ct);
}
