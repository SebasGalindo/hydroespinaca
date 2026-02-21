using HydroEspinaca.Shared.Abstractions;

namespace NotificationService.Domain.Entities;

// Estados de entrega que almacenamos para auditoría y monitoreo
public enum EmailDeliveryStatus { Queued, Sent, Failed }

// Documento de auditoría de envíos de correo
public class EmailLog : IIdentifiableMutable
{
    public required string Id { get; set; }
    public required string CorrelationId { get; init; }
    public required string To { get; init; }
    public string? Subject { get; init; }
    public EmailDeliveryStatus Status { get; set; }
    public string? Provider { get; set; }
    public string? ProviderMessageId { get; set; }
    public string? Error { get; set; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }
    public void SetId(string id) => Id = id;
}
