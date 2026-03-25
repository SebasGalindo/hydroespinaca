using HydroEspinaca.Shared.Abstractions;
using NotificationService.Domain.Entities;

namespace NotificationService.Domain.Persistence.Documents;

public class EmailLogDocument : IIdentifiableMutable
{
    public string Id { get; set; } = default!; // CorrelationId como Id
    public string CorrelationId { get; set; } = default!;
    public string To { get; set; } = default!;
    public string? Subject { get; set; }
    public EmailDeliveryStatus Status { get; set; }
    public string? Provider { get; set; }
    public string? ProviderMessageId { get; set; }
    public string? Error { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public void SetId(string id) => Id = id;
}
