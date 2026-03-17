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
        logger.LogInformation("[RAG] ═══ INICIO pipeline RAG: sesión={SessionId}, usuario={UserId}, mensaje={MsgLength} chars",
            request.SessionId, request.UserId, request.Message?.Length ?? 0);

        // 1. Cargar sesión y validar ownership
        logger.LogDebug("[RAG] Paso 1/8: Cargando sesión desde MongoDB...");
        var session = await sessionRepository.GetByIdAsync(request.SessionId, cancellationToken)
            ?? throw new InvalidOperationException($"Sesión {request.SessionId} no encontrada.");

        if (session.UserId != request.UserId)
            throw new UnauthorizedAccessException("La sesión no pertenece al usuario.");
        logger.LogDebug("[RAG] Sesión cargada: {MsgCount} mensajes previos", session.Messages.Count);

        // 2. Persistir el mensaje del usuario
        logger.LogDebug("[RAG] Paso 2/8: Persistiendo mensaje del usuario...");
        var userMessage = new ChatMessage
        {
            Role = "user",
            Content = request.Message,
            Timestamp = DateTime.UtcNow
        };
        await sessionRepository.AddMessageToSessionAsync(request.SessionId, userMessage, cancellationToken);

        // 3. Generar embedding de la pregunta (taskType = RETRIEVAL_QUERY)
        logger.LogDebug("[RAG] Paso 3/8: Generando embedding de la pregunta...");
        var historyForContext = session.Messages.TakeLast(MaxHistoryMessages).ToList();
        var textToVectorize = EnrichQueryWithContext(request.Message, historyForContext);
        
        logger.LogInformation("[RAG] Texto exacto enviado a vectorizar: '{Text}'", textToVectorize);
        
        var queryEmbedding = await embeddingProvider.GenerateEmbeddingAsync(
            textToVectorize, EmbeddingTaskType.RetrievalQuery, cancellationToken);
        logger.LogDebug("[RAG] Embedding generado: {Dims} dimensiones", queryEmbedding.Length);

        // 4. Buscar contexto vectorial (reglas fuzzy similares)
        logger.LogDebug("[RAG] Paso 4/8: Buscando contexto vectorial (Atlas Vector Search, topK={TopK})...", TopKRetrieval);
        var similarChunks = await vectorStore.SearchSimilarAsync(
            queryEmbedding, TopKRetrieval, null, cancellationToken);
        var vectorContext = BuildVectorContext(similarChunks);
        logger.LogInformation("[RAG] Contexto vectorial: {ChunkCount} chunks similares encontrados, {ContextLength} chars",
            similarChunks.Count, vectorContext.Length);

        // 5. Obtener datos en vivo (vía HTTP clients a cada microservicio)
        logger.LogDebug("[RAG] Paso 5/8: Obteniendo datos en vivo (sensor, fuzzy, actuador) en paralelo...");
        var liveHours = request.ContextFilters?.TimeRangeHours ?? DefaultLiveContextHours;

        var sensorTask = liveContextProvider.GetSensorReadingsSummaryAsync(liveHours, cancellationToken);
        var evaluationsTask = liveContextProvider.GetRecentEvaluationsSummaryAsync(liveHours, cancellationToken);
        var actuatorsTask = liveContextProvider.GetActuatorStatesSummaryAsync(cancellationToken);

        await Task.WhenAll(sensorTask, evaluationsTask, actuatorsTask);

        var sensorSummary = await sensorTask;
        var evaluationsSummary = await evaluationsTask;
        var actuatorSummary = await actuatorsTask;

        logger.LogInformation("[RAG] Datos en vivo obtenidos — sensores: {SensorLen} chars, fuzzy: {FuzzyLen} chars, actuadores: {ActLen} chars",
            sensorSummary?.Length ?? 0, evaluationsSummary?.Length ?? 0, actuatorSummary?.Length ?? 0);
        logger.LogDebug("[RAG] Sensor resumen: {Sensor}", sensorSummary?.Length > 200 ? sensorSummary[..200] + "..." : sensorSummary);
        logger.LogDebug("[RAG] Fuzzy resumen: {Fuzzy}", evaluationsSummary?.Length > 200 ? evaluationsSummary[..200] + "..." : evaluationsSummary);
        logger.LogDebug("[RAG] Actuador resumen: {Act}", actuatorSummary?.Length > 200 ? actuatorSummary[..200] + "..." : actuatorSummary);

        // 6. Ensamblar el prompt
        logger.LogDebug("[RAG] Paso 6/8: Ensamblando system instruction...");
        var systemInstruction = AssembleSystemInstruction(
            vectorContext, sensorSummary, evaluationsSummary, actuatorSummary);
        logger.LogInformation("[RAG] System instruction ensamblada: {InstructionLength} chars", systemInstruction.Length);
        logger.LogInformation("[RAG] Final System Instruction Prompt:\n{SystemInstruction}", systemInstruction);

        // 7. Preparar historial (últimos N mensajes)
        logger.LogDebug("[RAG] Paso 7/8: Preparando historial ({MaxHistory} máx)...", MaxHistoryMessages);
        var history = session.Messages
            .TakeLast(MaxHistoryMessages)
            .ToList();
        logger.LogDebug("[RAG] Historial preparado: {Count} mensajes", history.Count);

        // 8. Generar respuesta streaming y persistir
        logger.LogDebug("[RAG] Paso 8/8: Iniciando generación streaming vía Gemini + persistencia...");
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
        string? errorMessage = null;

        logger.LogDebug("[STREAM] Iterando tokens del LLM para sesión={SessionId}...", sessionId);

        await using (var enumerator = llmProvider.GenerateStreamAsync(systemInstruction, history, userMessage, ct).GetAsyncEnumerator(ct))
        {
            bool hasMore = true;
            while (hasMore)
            {
                string? token = null;
                try
                {
                    if (await enumerator.MoveNextAsync())
                    {
                        token = enumerator.Current;
                    }
                    else
                    {
                        hasMore = false;
                    }
                }
                catch (Exception ex) when (ex.GetType().Name.Contains("ServerError") || ex.Message.Contains("high demand"))
                {
                    logger.LogWarning(ex, "[STREAM] Alta demanda en API para sesión={SessionId}", sessionId);
                    errorMessage = "\n\n⚠️ **Aviso:** El servicio de Inteligencia Artificial está experimentando alta demanda en este momento. Por favor, intenta enviar tu pregunta nuevamente en unos minutos.";
                    hasMore = false;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "[STREAM] Error inesperado en API para sesión={SessionId}", sessionId);
                    errorMessage = "\n\n⚠️ **Error:** Ocurrió un problema inesperado al comunicarse con el modelo de IA. Por favor, inténtalo de nuevo.";
                    hasMore = false;
                }

                if (token != null)
                {
                    fullResponse.Append(token);
                    tokenCount++;
                    if (tokenCount <= 5)
                        logger.LogDebug("[STREAM] Token #{Num}: '{Token}'", tokenCount,
                            token.Length > 50 ? token[..50] + "..." : token);
                    yield return token;
                }
            }
        }

        if (errorMessage != null)
        {
            fullResponse.Append(errorMessage);
            yield return errorMessage;
        }

        logger.LogInformation("[STREAM] LLM streaming finalizado: sesión={SessionId}, tokens={Tokens}, respuesta={RespLen} chars",
            sessionId, tokenCount, fullResponse.Length);

        // Persistir la respuesta completa del modelo
        var modelMessage = new ChatMessage
        {
            Role = "model",
            Content = fullResponse.ToString(),
            Timestamp = DateTime.UtcNow,
            TokensUsed = tokenCount
        };
        await sessionRepository.AddMessageToSessionAsync(sessionId, modelMessage, ct);
        logger.LogDebug("[STREAM] Mensaje del modelo persistido en sesión={SessionId}", sessionId);

        // Auto-generar título si es el primer intercambio
        if (session.Messages.Count <= 1)
        {
            var title = userMessage.Length > 50 ? userMessage[..50] + "..." : userMessage;
            await sessionRepository.UpdateTitleAsync(sessionId, title, ct);
            logger.LogDebug("[STREAM] Título auto-generado: '{Title}'", title);
        }

        logger.LogInformation("[RAG] ═══ FIN pipeline RAG completado: sesión={SessionId}, tokens={Tokens}",
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

    private string EnrichQueryWithContext(string currentMessage, List<ChatMessage> history)
    {
        if (string.IsNullOrWhiteSpace(currentMessage)) return currentMessage;

        // Buscar en los últimos 4 mensajes del historial (de más reciente a más antiguo)
        var recentMessages = history.AsEnumerable().Reverse().Take(4);
        foreach (var msg in recentMessages)
        {
            if (string.IsNullOrWhiteSpace(msg.Content)) continue;

            // Busca patrones como: sistema "Control", sistema de Ventilacion, sistema Riego
            var match = System.Text.RegularExpressions.Regex.Match(
                msg.Content, 
                @"sistema\s+(?:de\s+)?(?:""([^""]+)""|'([^']+)'|([A-Za-z0-9_]+))", 
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            
            if (match.Success)
            {
                var sysName = match.Groups[1].Success ? match.Groups[1].Value :
                              match.Groups[2].Success ? match.Groups[2].Value :
                              match.Groups[3].Value;
                
                // Evitamos duplicar si el usuario ya lo mencionó en su mensaje actual
                if (!currentMessage.Contains(sysName, StringComparison.OrdinalIgnoreCase))
                {
                    logger.LogDebug("[RAG] Enriqueciendo consulta con contexto inferido: Sistema {SysName}", sysName);
                    return $"[Contexto implícito del historial: Sistema {sysName}] {currentMessage}";
                }
            }
        }
        return currentMessage;
    }
}
