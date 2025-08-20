using NotificationService.Domain.Models;

namespace NotificationService.Domain.Interfaces;

public interface IEmailLogRepository
{
    Task InsertAsync(EmailLog log, CancellationToken ct = default);
    Task UpdateStatusAsync(string correlationId, EmailDeliveryStatus status, string? provider = null, string? providerMessageId = null, string? error = null, CancellationToken ct = default);
}
