using ChatbotService.Domain.Interfaces;
using ChatbotService.Domain.Entities;
using ChatbotService.Infrastructure.Documents;
using ChatbotService.Infrastructure.Mappers;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace ChatbotService.Infrastructure.Repositories;

/// <summary>
/// Repositorio MongoDB para la colección <c>chat_sessions</c>.
/// Implementa <see cref="IChatSessionRepository"/> usando el patrón de composición
/// con <c>IMongoCollection</c> directamente para operaciones específicas.
/// </summary>
public class MongoChatSessionRepository : IChatSessionRepository
{
    private readonly IMongoCollection<ChatSessionDocument> _collection;
    private readonly ChatSessionMapper _mapper;
    private readonly ILogger<MongoChatSessionRepository> _logger;

    /// <summary>
    /// Inicializa el repositorio de sesiones de chat.
    /// </summary>
    /// <param name="database">Instancia de la base de datos MongoDB.</param>
    /// <param name="logger">Logger.</param>
    public MongoChatSessionRepository(IMongoDatabase database, ILogger<MongoChatSessionRepository> logger)
    {
        _collection = database.GetCollection<ChatSessionDocument>("chat_sessions");
        _mapper = new ChatSessionMapper();
        _logger = logger;

        // Crear índices al inicializar
        CreateIndexes();
    }

    /// <inheritdoc />
    public async Task<ChatSession?> GetByIdAsync(string sessionId, CancellationToken ct)
    {
        var doc = await _collection.Find(d => d.Id == sessionId).FirstOrDefaultAsync(ct);
        return doc != null ? _mapper.ToEntity(doc) : null;
    }

    /// <inheritdoc />
    public async Task CreateAsync(ChatSession session, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(session.Id))
            session.SetId(Guid.NewGuid().ToString());

        var doc = _mapper.ToDocument(session);
        await _collection.InsertOneAsync(doc, cancellationToken: ct);
        _logger.LogInformation("Sesión de chat creada: {SessionId}", session.Id);
    }

    /// <inheritdoc />
    public async Task<List<ChatSession>> GetSessionsByUserAsync(string userId, int skip, int limit, CancellationToken ct)
    {
        var filter = Builders<ChatSessionDocument>.Filter.Eq(d => d.UserId, userId)
                   & Builders<ChatSessionDocument>.Filter.Eq(d => d.IsArchived, false);
        var sort = Builders<ChatSessionDocument>.Sort.Descending(d => d.UpdatedAt);

        var docs = await _collection
            .Find(filter)
            .Sort(sort)
            .Skip(skip)
            .Limit(limit)
            .ToListAsync(ct);

        return docs.Select(d => _mapper.ToEntity(d)).ToList();
    }

    /// <inheritdoc />
    public async Task AddMessageToSessionAsync(string sessionId, ChatMessage message, CancellationToken ct)
    {
        var messageDoc = new ChatMessageDocument
        {
            Role = message.Role,
            Content = message.Content,
            Timestamp = message.Timestamp,
            TokensUsed = message.TokensUsed
        };

        var filter = Builders<ChatSessionDocument>.Filter.Eq(d => d.Id, sessionId);
        var update = Builders<ChatSessionDocument>.Update
            .Push(d => d.Messages, messageDoc)
            .Set(d => d.UpdatedAt, DateTime.UtcNow);

        var result = await _collection.UpdateOneAsync(filter, update, cancellationToken: ct);

        if (result.ModifiedCount == 0)
            _logger.LogWarning("No se encontró sesión {SessionId} para agregar mensaje", sessionId);
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(string sessionId, CancellationToken ct)
    {
        // Soft delete: archivar la sesión en vez de eliminarla
        var filter = Builders<ChatSessionDocument>.Filter.Eq(d => d.Id, sessionId);
        var update = Builders<ChatSessionDocument>.Update
            .Set(d => d.IsArchived, true)
            .Set(d => d.UpdatedAt, DateTime.UtcNow);

        var result = await _collection.UpdateOneAsync(filter, update, cancellationToken: ct);
        _logger.LogInformation("Sesión {SessionId} archivada: {Success}", sessionId, result.ModifiedCount > 0);
        return result.ModifiedCount > 0;
    }

    /// <inheritdoc />
    public async Task UpdateTitleAsync(string sessionId, string title, CancellationToken ct)
    {
        var filter = Builders<ChatSessionDocument>.Filter.Eq(d => d.Id, sessionId);
        var update = Builders<ChatSessionDocument>.Update
            .Set(d => d.Title, title)
            .Set(d => d.UpdatedAt, DateTime.UtcNow);

        await _collection.UpdateOneAsync(filter, update, cancellationToken: ct);
    }

    /// <summary>
    /// Crea los índices necesarios para la colección <c>chat_sessions</c>.
    /// </summary>
    private void CreateIndexes()
    {
        try
        {
            var indexModel = new CreateIndexModel<ChatSessionDocument>(
                Builders<ChatSessionDocument>.IndexKeys
                    .Ascending(d => d.UserId)
                    .Descending(d => d.UpdatedAt),
                new CreateIndexOptions { Name = "idx_user_updated" }
            );
            _collection.Indexes.CreateOne(indexModel);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error creando índices de chat_sessions (puede ya existir)");
        }
    }
}
