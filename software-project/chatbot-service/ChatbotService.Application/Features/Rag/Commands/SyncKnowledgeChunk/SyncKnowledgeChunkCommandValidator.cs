using FluentValidation;

namespace ChatbotService.Application.Features.Rag.Commands.SyncKnowledgeChunk;

/// <summary>
/// Validador para <see cref="SyncKnowledgeChunkCommand"/>.
/// </summary>
public class SyncKnowledgeChunkCommandValidator : AbstractValidator<SyncKnowledgeChunkCommand>
{
    /// <summary>
    /// Inicializa las reglas de validación.
    /// </summary>
    public SyncKnowledgeChunkCommandValidator()
    {
        RuleFor(x => x.SourceType)
            .NotEmpty().WithMessage("El SourceType es requerido.")
            .Must(x => x is "fuzzy_rule" or "fuzzy_system" or "fuzzy_variable" or "fuzzy_term" or "system_manual")
            .WithMessage("SourceType debe ser: fuzzy_rule, fuzzy_system, fuzzy_variable, fuzzy_term o system_manual.");

        RuleFor(x => x.SourceId)
            .NotEmpty().WithMessage("El SourceId es requerido.");

        RuleFor(x => x.Action)
            .NotEmpty().WithMessage("La Action es requerida.")
            .Must(x => x is "upsert" or "delete")
            .WithMessage("Action debe ser: upsert o delete.");
    }
}
