namespace NotificationService.Domain.Interfaces;

using NotificationService.Domain.Entities;

// Almacén distribuido de idempotencia (Mongo con TTL) para evitar envíos duplicados
public interface IIdempotencyStore
{
    Task<(bool acquired, IdempotencyRecord record)> TryReserveAsync(string key, string correlationId, TimeSpan ttl, CancellationToken ct = default);
    Task<IdempotencyRecord?> GetAsync(string key, CancellationToken ct = default);
    Task UpdateAsync(string key, Action<IdempotencyRecord> update, CancellationToken ct = default);
}
