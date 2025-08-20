using HydroEspinaca.Shared.Abstractions;

namespace NotificationService.Domain.Models;

public enum IdempotencyStatus { Reserved, Queued, Sent, Failed }

public sealed class IdempotencyRecord : IIdentifiableMutable
{
    public required string Key { get; init; }
    public required string CorrelationId { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset ExpiresAt { get; init; }
    public IdempotencyStatus Status { get; set; }
    public string? ResponseJson { get; set; }
    public string Id => Key;
    public void SetId(string id) { }
}
