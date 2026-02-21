using HydroEspinaca.Shared.Abstractions;
using NotificationService.Domain.Entities;

namespace NotificationService.Domain.Persistence.Documents;

public class IdempotencyDocument : IIdentifiableMutable
{
    public string Id { get; set; } = default!; // Key
    public string Key { get; set; } = default!;
    public string CorrelationId { get; set; } = default!;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public IdempotencyStatus Status { get; set; }
    public string? ResponseJson { get; set; }
    public void SetId(string id) { Id = id; Key = id; }
}
