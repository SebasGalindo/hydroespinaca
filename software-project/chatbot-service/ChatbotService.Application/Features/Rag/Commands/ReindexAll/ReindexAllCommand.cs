using MediatR;

namespace ChatbotService.Application.Features.Rag.Commands.ReindexAll;

/// <summary>
/// Comando administrativo para re-vectorizar toda la base de conocimientos.
/// Incluye: todas las reglas/sistemas/variables fuzzy + manuales del sistema.
/// </summary>
public record ReindexAllCommand() : IRequest<int>;
