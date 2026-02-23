using ChatbotService.Domain.Interfaces;
using ChatbotService.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ChatbotService.Application.Features.Chat.Commands.SendMessage;

/// <summary>
/// Handler principal que orquesta el flujo RAG completo:
/// 1. Validar y cargar historial de sesión
/// 2. Generar embedding de la pregunta
/// 3. Buscar contexto vectorial (Atlas Vector Search)
/// 4. Obtener datos en vivo (vía HTTP clients a cada microservicio)
/// 5. Ensamblar el prompt final
/// 6. Llamar a Gemini streaming
/// 7. Persistir pregunta + respuesta
/// 8. Retornar IAsyncEnumerable tokens (SSE)
/// </summary>
    public class SendMessageCommandHandler(
    IChatSessionRepository sessionRepository,
    IEmbeddingProvider embeddingProvider,
    IVectorStore vectorStore,
    ILiveContextProvider liveContextProvider,
    ILlmProvider llmProvider,
    ILogger<SendMessageCommandHandler> logger)
    : IRequestHandler<SendMessageCommand, IAsyncEnumerable<string>>
{
    /// <summary>
    /// Máximo de mensajes de historial a incluir en el prompt.
    /// </summary>
    private const int MaxHistoryMessages = 20;

    /// <summary>
    /// Top K de resultados para búsqueda vectorial.
    /// </summary>
    private const int TopKRetrieval = 5;

    /// <summary>
    /// Horas por defecto para datos en vivo.
    /// </summary>
    private const int DefaultLiveContextHours = 24;

    /// <inheritdoc />
    public async Task<IAsyncEnumerable<string>> Handle(SendMessageCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Procesando mensaje RAG: sesión={SessionId}, usuario={UserId}",
            request.SessionId, request.UserId);

        // 1. Cargar sesión y validar ownership
        var session = await sessionRepository.GetByIdAsync(request.SessionId, cancellationToken)
            ?? throw new InvalidOperationException($"Sesión {request.SessionId} no encontrada.");

        if (session.UserId != request.UserId)
            throw new UnauthorizedAccessException("La sesión no pertenece al usuario.");

        // 2. Persistir el mensaje del usuario
        var userMessage = new ChatMessage
        {
            Role = "user",
            Content = request.Message,
            Timestamp = DateTime.UtcNow
        };
        await sessionRepository.AddMessageToSessionAsync(request.SessionId, userMessage, cancellationToken);

        // 3. Generar embedding de la pregunta (taskType = RETRIEVAL_QUERY)
        var queryEmbedding = await embeddingProvider.GenerateEmbeddingAsync(
            request.Message, EmbeddingTaskType.RetrievalQuery, cancellationToken);

        // 4. Buscar contexto vectorial (reglas fuzzy similares)
        var similarChunks = await vectorStore.SearchSimilarAsync(
            queryEmbedding, TopKRetrieval, null, cancellationToken);
        var vectorContext = BuildVectorContext(similarChunks);

        // 5. Obtener datos en vivo (vía HTTP clients a cada microservicio)
        var liveHours = request.ContextFilters?.TimeRangeHours ?? DefaultLiveContextHours;

        var sensorTask = liveContextProvider.GetSensorReadingsSummaryAsync(liveHours, cancellationToken);
        var evaluationsTask = liveContextProvider.GetRecentEvaluationsSummaryAsync(liveHours, cancellationToken);
        var actuatorsTask = liveContextProvider.GetActuatorStatesSummaryAsync(cancellationToken);

        await Task.WhenAll(sensorTask, evaluationsTask, actuatorsTask);

        var sensorSummary = await sensorTask;
        var evaluationsSummary = await evaluationsTask;
        var actuatorSummary = await actuatorsTask;

        // 6. Ensamblar el prompt
        var systemInstruction = AssembleSystemInstruction(
            vectorContext, sensorSummary, evaluationsSummary, actuatorSummary);

        // 7. Preparar historial (últimos N mensajes)
        var history = session.Messages
            .TakeLast(MaxHistoryMessages)
            .ToList();

        // 8. Generar respuesta streaming y persistir
        return StreamAndPersist(request.SessionId, session, systemInstruction, history, request.Message, cancellationToken);
    }

    /// <summary>
    /// Genera la respuesta vía streaming de Gemini, acumula el texto completo,
    /// y persiste el mensaje final en la sesión.
    /// </summary>
    private async IAsyncEnumerable<string> StreamAndPersist(
        string sessionId,
        ChatSession session,
        string systemInstruction,
        List<ChatMessage> history,
        string userMessage,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
    {
        var fullResponse = new System.Text.StringBuilder();
        var tokenCount = 0;

        await foreach (var token in llmProvider.GenerateStreamAsync(systemInstruction, history, userMessage, ct))
        {
            fullResponse.Append(token);
            tokenCount++;
            yield return token;
        }

        // Persistir la respuesta completa del modelo
        var modelMessage = new ChatMessage
        {
            Role = "model",
            Content = fullResponse.ToString(),
            Timestamp = DateTime.UtcNow,
            TokensUsed = tokenCount
        };
        await sessionRepository.AddMessageToSessionAsync(sessionId, modelMessage, ct);

        // Auto-generar título si es el primer intercambio
        if (session.Messages.Count <= 1)
        {
            var title = userMessage.Length > 50 ? userMessage[..50] + "..." : userMessage;
            await sessionRepository.UpdateTitleAsync(sessionId, title, ct);
        }

        logger.LogInformation("Respuesta RAG completada: sesión={SessionId}, tokens={Tokens}",
            sessionId, tokenCount);
    }

    private const string SystemInstructionTemplate = @"Eres el Asistente Inteligente de HydroEspinaca, un sistema de hidroponia automatizado.
Tu rol es ayudar al usuario a:
1. Crear y optimizar reglas de lógica difusa para controlar actuadores (ventilador, bomba, termocalefactor, etc.)
2. Analizar datos históricos de sensores y sugerir ajustes.
3. Responder preguntas sobre el estado actual del sistema.

REGLAS ESTRICTAS:
- Responde SOLO basándote en el contexto proporcionado. Si no tienes datos suficientes, dilo explícitamente.
- Cuando sugieras una regla fuzzy, usa el formato exacto: ""SI [variable] ES [término] ENTONCES [variable_salida] = [término]"".
- Usa Markdown para formatear respuestas. Usa bloques de código para JSON o configuraciones.
- Responde en español.
- No inventes datos de sensores. Si los datos en el contexto no cubren la pregunta, sugiere al usuario qué datos recopilar.
- Sé conciso pero completo. Prioriza la claridad y la accionabilidad de la respuesta.";

    private static string AssembleSystemInstruction(
        string vectorContext,
        string sensorSummary,
        string evaluationsSummary,
        string actuatorSummary)
    {
        var sections = new List<string> { SystemInstructionTemplate };

        if (!string.IsNullOrWhiteSpace(vectorContext))
        {
            sections.Add($@"
CONTEXTO DEL SISTEMA (reglas fuzzy y conocimiento base):
{vectorContext}");
        }

        var liveContextParts = new List<string>();
        if (!string.IsNullOrWhiteSpace(sensorSummary) && !sensorSummary.StartsWith("[Sin"))
            liveContextParts.Add(sensorSummary);
        if (!string.IsNullOrWhiteSpace(evaluationsSummary) && !evaluationsSummary.StartsWith("[Sin"))
            liveContextParts.Add(evaluationsSummary);
        if (!string.IsNullOrWhiteSpace(actuatorSummary) && !actuatorSummary.StartsWith("[Sin"))
            liveContextParts.Add(actuatorSummary);

        if (liveContextParts.Count > 0)
        {
            sections.Add($@"
DATOS EN TIEMPO REAL:
{string.Join("\n\n", liveContextParts)}");
        }

        return string.Join("\n", sections);
    }

    private static string BuildVectorContext(List<KnowledgeChunk> chunks)
    {
        if (chunks.Count == 0)
            return string.Empty;

        var lines = chunks.Select((c, i) => $"[{i + 1}] ({c.SourceType}) {c.Content}");
        return string.Join("\n\n", lines);
    }
}
