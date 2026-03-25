using ChatbotService.Domain.Entities;

namespace ChatbotService.Domain.Interfaces;

/// <summary>
/// Provee capacidades de generación de lenguaje e integración con la API de Google Gemini (gemini-3-flash-preview).
/// </summary>
public interface ILlmProvider
{
    /// <summary>
    /// Genera la respuesta en streaming basada en un contexto armado, historial y la pregunta del usuario. Devuelve el JSON text (Structured Output).
    /// </summary>
    /// <param name="systemInstruction">Instrucción inicial con restricciones, rol principal de HydroEspinaca y tools.</param>
    /// <param name="history">Listado del histórico de la conversación desde MongoDB.</param>
    /// <param name="userMessage">Mensaje reciente que envió el cliente.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>Texto estructurado por un JSON parseable.</returns>
    IAsyncEnumerable<string> GenerateStreamAsync(string systemInstruction, List<ChatMessage> history, string userMessage, CancellationToken ct);
}
