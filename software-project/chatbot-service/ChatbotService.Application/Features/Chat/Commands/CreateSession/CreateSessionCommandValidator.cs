using FluentValidation;

namespace ChatbotService.Application.Features.Chat.Commands.CreateSession;

/// <summary>
/// Validador para <see cref="CreateSessionCommand"/>.
/// </summary>
public class CreateSessionCommandValidator : AbstractValidator<CreateSessionCommand>
{
    /// <summary>
    /// Inicializa las reglas de validación.
    /// </summary>
    public CreateSessionCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("El UserId es requerido.");
    }
}
