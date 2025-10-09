using ActuatorService.Domain.Entities;
using ActuatorService.Domain.Interfaces;
using ActuatorService.Infrastructure.Persistence.Models;
using HydroEspinaca.Shared.Mongo;
using HydroEspinaca.Shared.Mongo.Interfaces;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace ActuatorService.Infrastructure.Services;

/// <summary>
/// Manages temporary blocks (cooldowns) on actuators
/// </summary>
public class PinBlockManager : IPinBlockManager
{
    private readonly BaseMongoRepository<ActuatorCooldown, ActuatorCooldownDocument> _baseRepo;
    private readonly IMongoCollection<ActuatorCooldownDocument> _collection;
    private readonly ILogger<PinBlockManager> _logger;

    public PinBlockManager(
        MongoDbContext ctx,
        IEntityMapper<ActuatorCooldown, ActuatorCooldownDocument> mapper,
        ILogger<PinBlockManager> logger)
    {
        _baseRepo = new BaseMongoRepository<ActuatorCooldown, ActuatorCooldownDocument>(
            ctx.Database, "actuator_cooldowns", mapper);
        _collection = ctx.Database.GetCollection<ActuatorCooldownDocument>("actuator_cooldowns");
        _logger = logger;
    }

    public async Task<bool> IsBlockedAsync(string actuatorId)
    {
        var now = DateTime.UtcNow;
        var filter = Builders<ActuatorCooldownDocument>.Filter.And(
            Builders<ActuatorCooldownDocument>.Filter.Eq(c => c.ActuatorId, actuatorId),
            Builders<ActuatorCooldownDocument>.Filter.Eq(c => c.IsActive, true),
            Builders<ActuatorCooldownDocument>.Filter.Gt(c => c.ExpiresAt, now)
        );

        var cooldown = await _collection.Find(filter).FirstOrDefaultAsync();
        return cooldown != null;
    }

    public async Task<ActuatorCooldownInfo?> GetCooldownInfoAsync(string actuatorId)
    {
        var now = DateTime.UtcNow;
        var filter = Builders<ActuatorCooldownDocument>.Filter.And(
            Builders<ActuatorCooldownDocument>.Filter.Eq(c => c.ActuatorId, actuatorId),
            Builders<ActuatorCooldownDocument>.Filter.Eq(c => c.IsActive, true),
            Builders<ActuatorCooldownDocument>.Filter.Gt(c => c.ExpiresAt, now)
        );

        var cooldown = await _collection.Find(filter).FirstOrDefaultAsync();
        if (cooldown == null)
            return null;

        return new ActuatorCooldownInfo
        {
            ActuatorId = cooldown.ActuatorId,
            Reason = cooldown.Reason,
            StartedAt = cooldown.StartedAt,
            ExpiresAt = cooldown.ExpiresAt,
            RemainingTime = cooldown.ExpiresAt - now
        };
    }

    public async Task BlockForAsync(string actuatorId, TimeSpan duration, string reason)
    {
        var now = DateTime.UtcNow;

        // Deactivate any existing cooldowns for this actuator
        var deactivateFilter = Builders<ActuatorCooldownDocument>.Filter.And(
            Builders<ActuatorCooldownDocument>.Filter.Eq(c => c.ActuatorId, actuatorId),
            Builders<ActuatorCooldownDocument>.Filter.Eq(c => c.IsActive, true)
        );

        var deactivateUpdate = Builders<ActuatorCooldownDocument>.Update.Set(c => c.IsActive, false);
        await _collection.UpdateManyAsync(deactivateFilter, deactivateUpdate);

        // Create new cooldown
        var cooldown = new ActuatorCooldown
        {
            Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(),
            ActuatorId = actuatorId,
            Reason = reason,
            StartedAt = now,
            ExpiresAt = now.Add(duration),
            IsActive = true
        };

        await _baseRepo.CreateAsync(cooldown);

        _logger.LogWarning("🔒 Actuator {ActuatorId} blocked for {Duration}. Reason: {Reason}",
            actuatorId, duration, reason);
    }

    public async Task CleanupExpiredCooldownsAsync()
    {
        var now = DateTime.UtcNow;
        var filter = Builders<ActuatorCooldownDocument>.Filter.And(
            Builders<ActuatorCooldownDocument>.Filter.Eq(c => c.IsActive, true),
            Builders<ActuatorCooldownDocument>.Filter.Lt(c => c.ExpiresAt, now)
        );

        var update = Builders<ActuatorCooldownDocument>.Update.Set(c => c.IsActive, false);
        var result = await _collection.UpdateManyAsync(filter, update);

        if (result.ModifiedCount > 0)
        {
            _logger.LogInformation("🔓 Cleaned up {Count} expired cooldowns", result.ModifiedCount);
        }
    }

    public async Task<List<ActuatorCooldownInfo>> GetAllActiveCooldownsAsync()
    {
        var now = DateTime.UtcNow;
        var filter = Builders<ActuatorCooldownDocument>.Filter.And(
            Builders<ActuatorCooldownDocument>.Filter.Eq(c => c.IsActive, true),
            Builders<ActuatorCooldownDocument>.Filter.Gt(c => c.ExpiresAt, now)
        );

        var cooldowns = await _collection.Find(filter).ToListAsync();

        return cooldowns.Select(c => new ActuatorCooldownInfo
        {
            ActuatorId = c.ActuatorId,
            Reason = c.Reason,
            StartedAt = c.StartedAt,
            ExpiresAt = c.ExpiresAt,
            RemainingTime = c.ExpiresAt - now
        }).ToList();
    }
}
