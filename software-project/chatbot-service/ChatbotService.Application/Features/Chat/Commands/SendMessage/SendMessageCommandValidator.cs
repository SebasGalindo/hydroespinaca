using FluentValidation;

namespace ChatbotService.Application.Features.Chat.Commands.SendMessage;

/// <summary>
/// Validador para <see cref="SendMessageCommand"/>.
/// </summary>
public class SendMessageCommandValidator : AbstractValidator<SendMessageCommand>
{
    /// <summary>
    /// Inicializa las reglas de validación.
    /// </summary>
    public SendMessageCommandValidator()
    {
        RuleFor(x => x.SessionId)
            .NotEmpty().WithMessage("El SessionId es requerido.");

        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("El UserId es requerido.");

        RuleFor(x => x.Message)
            .NotEmpty().WithMessage("El mensaje no puede estar vacío.")
            .MaximumLength(4000).WithMessage("El mensaje no puede exceder 4000 caracteres.");
    }
}
