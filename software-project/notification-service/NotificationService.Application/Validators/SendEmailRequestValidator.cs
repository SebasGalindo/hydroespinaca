using FluentValidation;
using HydroEspinaca.Shared.DTOs.Notifications;

namespace NotificationService.Application.Validators;

// Valida la entrada del envío de correo (seguridad, tamaños y formatos válidos)
public class SendEmailRequestValidator : AbstractValidator<SendEmailRequestDto>
{
    private const int MaxSubjectLength = 200;
    private const int MaxHtmlLength = 100_000; // ~100KB
    private const int MaxAttachmentBytes = 5 * 1024 * 1024; // 5MB individual
    private const int MaxTotalAttachmentsBytes = 10 * 1024 * 1024; // 10MB total

    public SendEmailRequestValidator()
    {
        // Validar que solo una opción esté presente (Group o To)
        RuleFor(x => x)
            .Must(x => !string.IsNullOrWhiteSpace(x.Group) || !string.IsNullOrWhiteSpace(x.To))
            .WithMessage("Either 'Group' or 'To' field must be specified")
            .Must(x => string.IsNullOrWhiteSpace(x.Group) || string.IsNullOrWhiteSpace(x.To))
            .WithMessage("Cannot specify both 'Group' and 'To' fields. Choose one sending mode.");

        // Validación para envío directo (cuando To está presente)
        When(x => !string.IsNullOrWhiteSpace(x.To), () =>
        {
            RuleFor(x => x.To).NotEmpty().EmailAddress();
            RuleForEach(x => x.Cc).NotEmpty().EmailAddress().When(x => x.Cc != null);
            RuleForEach(x => x.Bcc).NotEmpty().EmailAddress().When(x => x.Bcc != null);
        });

        // Validación para envío por grupo (cuando Group está presente)
        When(x => !string.IsNullOrWhiteSpace(x.Group), () =>
        {
            RuleFor(x => x.Group)
                .NotEmpty()
                .Length(1, 100)
                .WithMessage("Group name must be between 1 and 100 characters")
                .Matches(@"^[a-zA-Z0-9\-_\s]+$")
                .WithMessage("Group name can only contain letters, numbers, hyphens, underscores and spaces");
            
            // Cuando se usa Group, To/Cc/Bcc deben estar vacíos
            RuleFor(x => x.Cc)
                .Must(cc => cc == null || cc.Length == 0)
                .WithMessage("Cannot specify 'Cc' when using 'Group' mode");
            
            RuleFor(x => x.Bcc)
                .Must(bcc => bcc == null || bcc.Length == 0)
                .WithMessage("Cannot specify 'Bcc' when using 'Group' mode");
        });

        // Asunto no vacío y con límite razonable
        RuleFor(x => x.Subject)
            .NotEmpty()
            .MaximumLength(MaxSubjectLength);

        // HTML del cuerpo limitado para evitar abusos
        RuleFor(x => x.HtmlBody)
            .NotEmpty()
            .MaximumLength(MaxHtmlLength);

        When(x => x.Attachments != null && x.Attachments.Count > 0, () =>
        {
            // Validación de adjuntos: nombre, tipo y base64 válido
            RuleForEach(x => x.Attachments!)
                .ChildRules(a =>
                {
                    a.RuleFor(p => p.FileName).NotEmpty().MaximumLength(255);
                    a.RuleFor(p => p.ContentType).NotEmpty().MaximumLength(100);
                    a.RuleFor(p => p.ContentBase64)
                        .NotEmpty()
                        .Must(IsBase64)
                        .WithMessage("Attachment content must be Base64");
                });

            // Límite total de adjuntos (10MB)
            RuleFor(x => x.Attachments!)
                .Must(list => list.Sum(att => GetBase64Length(att.ContentBase64)) <= MaxTotalAttachmentsBytes)
                .WithMessage("Total attachments size exceeds limit");

            // Límite por archivo (5MB)
            RuleForEach(x => x.Attachments!)
                .Must(att => GetBase64Length(att.ContentBase64) <= MaxAttachmentBytes)
                .WithMessage("Attachment exceeds per-file size limit");
        });
    }

    private static bool IsBase64(string s)
    {
        Span<byte> buffer = stackalloc byte[s.Length];
        return Convert.TryFromBase64String(s, buffer, out _);
    }

    private static int GetBase64Length(string base64)
    {
        try
        {
            return Convert.FromBase64String(base64).Length;
        }
        catch { return int.MaxValue; }
    }
}
